using Microsoft.EntityFrameworkCore;

namespace PourCast.Scoring.Data;

/// <summary>
/// Database context for recorded forecasts and observations.
/// </summary>
/// <param name="options">The database context options.</param>
public class ScoringDbContext(DbContextOptions<ScoringDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Recorded forecasts, one per source per snapshot time.
    /// </summary>
    public DbSet<Snapshot> Snapshots => Set<Snapshot>();

    /// <summary>
    /// Predicted hours from the recorded forecasts.
    /// </summary>
    public DbSet<Prediction> Predictions => Set<Prediction>();

    /// <summary>
    /// Observed hours from the METAR station.
    /// </summary>
    public DbSet<Observation> Observations => Set<Observation>();

    /// <summary>
    /// Configures keys and indexes.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Snapshot>().HasIndex(x => new { x.Source, x.IssuedAt }).IsUnique();
        modelBuilder.Entity<Snapshot>().Property(x => x.Source).HasMaxLength(64);
        modelBuilder.Entity<Prediction>().HasIndex(x => x.Time);
        modelBuilder.Entity<Prediction>().HasOne(x => x.Snapshot).WithMany(x => x.Predictions).HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Observation>().HasIndex(x => new { x.Station, x.Time }).IsUnique();
        modelBuilder.Entity<Observation>().Property(x => x.Station).HasMaxLength(8);
    }
}