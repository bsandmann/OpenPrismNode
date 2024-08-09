namespace OpenPrismNode.Core;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class DataContext : DbContext
{
    protected readonly IConfiguration Configuration;

    // Constructor that accepts DbContextOptions
    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    // DbSet property
    public DbSet<NetworkEntity> PrismNetworkEntities { get; set; }
    public DbSet<EpochEntity> EpochEntities { get; set; }
    public DbSet<BlockEntity> BlockEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NetworkEntity>().HasKey(p => p.NetworkType);
        modelBuilder.Entity<EpochEntity>().HasKey(p => p.EpochNumber);
        modelBuilder.Entity<BlockEntity>()
            .HasKey(b => new { b.BlockHeight, b.BlockHashPrefix });

        modelBuilder.Entity<EpochEntity>()
            .HasOne(p => p.NetworkEntity)
            .WithMany(b => b.Epochs)
            .HasForeignKey(p => p.NetworkType)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlockEntity>()
            .HasOne(p => p.EpochEntity)
            .WithMany(b => b.BlockEntities)
            .HasForeignKey(p => p.EpochNumber)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlockEntity>()
            .HasOne(p => p.EpochEntity)
            .WithMany(b => b.BlockEntities)
            .HasForeignKey(p => p.EpochNumber)
            .OnDelete(DeleteBehavior.Cascade);

        // modelBuilder.Entity<PrismBlockEntity>()
        //     .HasOne(p => p.PreviousBlock)
        //     .WithMany(b => b.NextBlocks)
        //     .HasForeignKey(p => p.PreviousBlockHash)
        //     .OnDelete(DeleteBehavior.NoAction);
    }
}