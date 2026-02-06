using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarpetOps.Sync;
using Dapper;
using Microsoft.Data.SqlClient;
using Npgsql;

var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env");
envPath = Path.GetFullPath(envPath);

if (!File.Exists(envPath))
{
    Console.Error.WriteLine($"ERROR: .env not found at {envPath}");
    Environment.Exit(1);
}

DotNetEnv.Env.Load(envPath);

var sourceConn = RequireEnv("SOURCE_SQLSERVER_CONNECTION");
var targetConn = RequireEnv("APP_DB_CONNECTION");
var startDate = ParseDate(RequireEnv("SYNC_START_DATE"));
var batchSize = ParseInt(RequireEnv("SYNC_BATCH_SIZE"), 1000);

Console.WriteLine("=== CarpetOps.Sync ===");
Console.WriteLine($"Source (SQL Server) : {MaskConnectionString(sourceConn)}");
Console.WriteLine($"Target (PostgreSQL) : {MaskConnectionString(targetConn)}");
Console.WriteLine($"Start date          : {startDate:yyyy-MM-dd}");
Console.WriteLine($"Batch size          : {batchSize}");
Console.WriteLine();
Console.WriteLine("[1/3] Ensuring Postgres table...");

const string createTableSql = """
    CREATE TABLE IF NOT EXISTS erp_barcode_items (
        barcode_no   TEXT NOT NULL,
        orno         TEXT NOT NULL,
        design_name  TEXT,
        list_no      TEXT,
        item_no      TEXT,
        cnv_id       TEXT,
        cnv_desc     TEXT,
        asplan       TEXT,
        width        NUMERIC(12,4),
        length       NUMERIC(12,4),
        sqm          NUMERIC(12,4),
        qty          NUMERIC(12,4),
        order_type   TEXT,
        row_hash     TEXT NOT NULL,
        synced_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
        PRIMARY KEY (barcode_no, orno, item_no)
    );
    """;

using (var pg = new NpgsqlConnection(targetConn))
{
    await pg.OpenAsync();
    await pg.ExecuteAsync(createTableSql);
}

Console.WriteLine("  Table erp_barcode_items ready.");

Console.WriteLine("[2/3] Fetching from SQL Server...");

const string fetchSql = """
    SELECT C.HT_BARCODE AS BarcodeNo,
           A.ORNO,
           B.PDNM AS DesignName,
           B.PD_ITEM AS ListNo,
           C.ITEM_NO AS ItemNo,
           B.CnvID,
           B.CnvDesc,
           B.AsPlan AS ASPLAN,
           B.PD_WIDTH AS Width,
           B.PD_LEN AS Length,
           C.SQM AS Sqm,
           C.Qty AS Qty,
           CASE WHEN A.ORTP = '0' THEN 'Order' ELSE 'Sample' END AS OrderType
    FROM CARPET.DBO.ORHMAIN A
    LEFT JOIN CARPET.DBO.ORDMAIN B ON A.AUTO = B.AUTO
    LEFT JOIN CARPET.DBO.HT_SCANBARCODE C ON C.OF_NO = A.ORNO
    WHERE A.ORDT >= @StartDate
      AND C.HT_BARCODE IS NOT NULL
      AND ISNULL(B.CnvID, '') <> ''
    """;

List<ErpBarcodeItemRow> rows;

using (var sqlConn = new SqlConnection(sourceConn))
{
    await sqlConn.OpenAsync();
    var result = await sqlConn.QueryAsync<ErpBarcodeItemRow>(fetchSql, new { StartDate = startDate });
    rows = result.ToList();
}

Console.WriteLine($"  Fetched {rows.Count} rows.");

if (rows.Count == 0)
{
    Console.WriteLine("Nothing to sync.");
    return;
}

Console.WriteLine("[3/3] Upserting into Postgres...");

const string upsertSql = """
    INSERT INTO erp_barcode_items
        (barcode_no, orno, design_name, list_no, item_no,
         cnv_id, cnv_desc, asplan, width, length, sqm, qty,
         order_type, row_hash, synced_at)
    VALUES
        (@BarcodeNo, @Orno, @DesignName, @ListNo, @ItemNo,
         @CnvID, @CnvDesc, @ASPLAN, @Width, @Length, @Sqm, @Qty,
         @OrderType, @RowHash, now())
    ON CONFLICT (barcode_no, orno, item_no) DO UPDATE SET
        design_name = EXCLUDED.design_name,
        list_no     = EXCLUDED.list_no,
        cnv_id      = EXCLUDED.cnv_id,
        cnv_desc    = EXCLUDED.cnv_desc,
        asplan      = EXCLUDED.asplan,
        width       = EXCLUDED.width,
        length      = EXCLUDED.length,
        sqm         = EXCLUDED.sqm,
        qty         = EXCLUDED.qty,
        order_type  = EXCLUDED.order_type,
        row_hash    = EXCLUDED.row_hash,
        synced_at   = now()
    WHERE erp_barcode_items.row_hash <> EXCLUDED.row_hash
    """;

int totalInserted = 0;
int totalUpdated = 0;
int batchNum = 0;

using var pg2 = new NpgsqlConnection(targetConn);
await pg2.OpenAsync();

foreach (var batch in rows.Chunk(batchSize))
{
    batchNum++;
    using var tx = await pg2.BeginTransactionAsync();

    int affected = 0;
    foreach (var row in batch)
    {
        TrimRow(row);
        var hash = ComputeHash(row);
        affected += await pg2.ExecuteAsync(upsertSql, new
        {
            row.BarcodeNo,
            row.Orno,
            row.DesignName,
            row.ListNo,
            row.ItemNo,
            row.CnvID,
            row.CnvDesc,
            row.ASPLAN,
            row.Width,
            row.Length,
            row.Sqm,
            row.Qty,
            row.OrderType,
            RowHash = hash
        }, tx);
    }

    await tx.CommitAsync();
    totalInserted += batch.Length;
    totalUpdated += affected;
    Console.WriteLine($"  Batch {batchNum}: {batch.Length} sent, {affected} written (insert or update).");
}

Console.WriteLine();
Console.WriteLine($"Sync complete. Processed={totalInserted}, Written={totalUpdated} (skipped {totalInserted - totalUpdated} unchanged).");

static string RequireEnv(string key)
{
    var val = Environment.GetEnvironmentVariable(key);
    if (string.IsNullOrWhiteSpace(val))
    {
        Console.Error.WriteLine($"ERROR: Missing required env var: {key}");
        Environment.Exit(1);
    }
    return val!;
}

static DateTime ParseDate(string value)
{
    if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date))
        return date;

    Console.Error.WriteLine($"ERROR: Invalid date format for SYNC_START_DATE: '{value}' (expected yyyy-MM-dd)");
    Environment.Exit(1);
    return default;
}

static int ParseInt(string value, int fallback)
{
    return int.TryParse(value, out var result) ? result : fallback;
}

static string MaskConnectionString(string conn)
{
    var parts = conn.Split(';');
    for (int i = 0; i < parts.Length; i++)
    {
        var kv = parts[i].Split('=', 2);
        if (kv.Length == 2 && kv[0].Trim().Equals("Password", StringComparison.OrdinalIgnoreCase))
            parts[i] = $"{kv[0]}=***";
    }
    return string.Join(';', parts);
}

static void TrimRow(ErpBarcodeItemRow r)
{
    r.BarcodeNo = r.BarcodeNo.Trim();
    r.Orno = r.Orno.Trim();
    r.DesignName = r.DesignName?.Trim();
    r.ListNo = r.ListNo?.Trim();
    r.ItemNo = r.ItemNo?.Trim();
    r.CnvID = r.CnvID?.Trim();
    r.CnvDesc = r.CnvDesc?.Trim();
    r.ASPLAN = r.ASPLAN?.Trim();
    r.OrderType = r.OrderType?.Trim();
}

static string ComputeHash(ErpBarcodeItemRow r)
{
    var ci = CultureInfo.InvariantCulture;
    var raw = string.Join("|",
        r.BarcodeNo,
        r.Orno,
        r.DesignName ?? "",
        r.ListNo ?? "",
        r.ItemNo ?? "",
        r.CnvID ?? "",
        r.CnvDesc ?? "",
        r.ASPLAN ?? "",
        r.Width?.ToString("F4", ci) ?? "",
        r.Length?.ToString("F4", ci) ?? "",
        r.Sqm?.ToString("F4", ci) ?? "",
        r.Qty?.ToString("F4", ci) ?? "",
        r.OrderType ?? "");

    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}
