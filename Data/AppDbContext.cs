using Microsoft.EntityFrameworkCore;
using Carpet_Work_Progress.Models;

namespace Carpet_Work_Progress.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProgressLog> ProgressLogs => Set<ProgressLog>();
    public DbSet<ErpBarcodeItem> ErpBarcodeItems => Set<ErpBarcodeItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProgressLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.NormalPercent).HasColumnType("decimal(5,2)");
            entity.Property(e => e.OtPercent).HasColumnType("decimal(5,2)");
            entity.Property(e => e.TotalPercent).HasColumnType("decimal(5,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => e.Barcode);
            entity.HasIndex(e => e.ProgressDate);
        });

        modelBuilder.Entity<ErpBarcodeItem>(entity =>
        {
            entity.ToTable("erp_barcode_items");
            entity.HasKey(e => e.BarcodeNo);
            entity.Property(e => e.BarcodeNo).HasColumnName("barcode_no");
            entity.Property(e => e.Orno).HasColumnName("orno");
            entity.Property(e => e.DesignName).HasColumnName("design_name");
            entity.Property(e => e.ListNo).HasColumnName("list_no");
            entity.Property(e => e.ItemNo).HasColumnName("item_no");
            entity.Property(e => e.CnvId).HasColumnName("cnv_id");
            entity.Property(e => e.CnvDesc).HasColumnName("cnv_desc");
            entity.Property(e => e.AsPlan).HasColumnName("asplan");
            entity.Property(e => e.Width).HasColumnName("width").HasColumnType("numeric(12,4)");
            entity.Property(e => e.Length).HasColumnName("length").HasColumnType("numeric(12,4)");
            entity.Property(e => e.Sqm).HasColumnName("sqm").HasColumnType("numeric(12,4)");
            entity.Property(e => e.Qty).HasColumnName("qty").HasColumnType("numeric(12,4)");
            entity.Property(e => e.OrderType).HasColumnName("order_type");
            entity.Property(e => e.SyncedAt).HasColumnName("synced_at");
        });
    }
}
