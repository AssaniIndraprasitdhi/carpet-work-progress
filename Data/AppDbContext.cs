using Microsoft.EntityFrameworkCore;
using Carpet_Work_Progress.Models;

namespace Carpet_Work_Progress.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProgressLog> ProgressLogs => Set<ProgressLog>();

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
    }
}
