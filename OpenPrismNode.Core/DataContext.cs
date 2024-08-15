namespace OpenPrismNode.Core;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models;

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
    public DbSet<TransactionEntity> TransactionEntities { get; set; }
    public DbSet<UtxoEntity> UtxoEntities { get; set; }
    public DbSet<StakeAddressEntity> StakeAddressEntities { get; set; }
    public DbSet<WalletAddressEntity> WalletAddressEntities { get; set; }
    public DbSet<CreateDidEntity> CreateDidEntities { get; set; }
    public DbSet<PrismPublicKeyEntity> PrismPublicKeyEntities { get; set; }
    public DbSet<PrismServiceEntity> PrismServiceEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NetworkEntity>().HasKey(p => p.NetworkType);
        modelBuilder.Entity<EpochEntity>().HasKey(p => p.EpochNumber);
        modelBuilder.Entity<BlockEntity>()
            .HasKey(b => new { b.BlockHeight, b.BlockHashPrefix });
        modelBuilder.Entity<TransactionEntity>()
            .HasKey(t => new { t.TransactionHash, t.BlockHeight, t.BlockHashPrefix });
        modelBuilder.Entity<UtxoEntity>().HasKey(p => p.UtxoEntityId);
        modelBuilder.Entity<CreateDidEntity>().HasKey(p => p.OperationHash);
        modelBuilder.Entity<PrismPublicKeyEntity>().HasKey(p => p.PrismPublicKeyEntityId);
        modelBuilder.Entity<PrismServiceEntity>().HasKey(p => p.PrismServiceEntityId);

        modelBuilder.Entity<EpochEntity>()
            .HasOne(p => p.NetworkEntity)
            .WithMany(b => b.Epochs)
            .HasForeignKey(p => p.NetworkType)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EpochEntity>()
            .Property(e => e.EpochNumber)
            .ValueGeneratedNever();

        modelBuilder.Entity<BlockEntity>()
            .HasOne(p => p.EpochEntity)
            .WithMany(b => b.BlockEntities)
            .HasForeignKey(p => p.EpochNumber)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlockEntity>()
            .HasOne(p => p.PreviousBlock)
            .WithMany(b => b.NextBlocks)
            .HasForeignKey(p => new { p.PreviousBlockHeight, p.PreviousBlockHashPrefix })
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TransactionEntity>()
            .HasOne(t => t.BlockEntity)
            .WithMany(b => b.PrismTransactionEntities)
            .HasForeignKey(t => new { t.BlockHeight, t.BlockHashPrefix })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TransactionEntity>()
            .HasIndex(t => t.TransactionHash)
            .IsUnique();

        modelBuilder.Entity<UtxoEntity>()
            .HasOne(u => u.Transaction)
            .WithMany(t => t.Utxos)
            .HasForeignKey(u => new { u.TransactionHash, u.BlockHeight, u.BlockHashPrefix })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UtxoEntity>()
            .HasIndex(u => new { u.TransactionHash, u.BlockHeight, u.BlockHashPrefix, u.Index, u.IsOutgoing, u.Value })
            .IsUnique();

        modelBuilder.Entity<UtxoEntity>()
            .HasOne(u => u.StakeAddressEntity)
            .WithMany(s => s.Utxos)
            .HasForeignKey(u => u.StakeAddress)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UtxoEntity>()
            .HasOne(u => u.WalletAddressEntity)
            .WithMany(s => s.Utxos)
            .HasForeignKey(u => u.WalletAddress)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StakeAddressEntity>(entity =>
        {
            entity.HasKey(s => s.StakeAddress);
            entity.Property(s => s.StakeAddress).HasMaxLength(64);
        });

        modelBuilder.Entity<WalletAddressEntity>(entity =>
        {
            entity.HasKey(w => w.WalletAddress);
            entity.Property(w => w.WalletAddress).HasMaxLength(114);
        });

        modelBuilder.Entity<CreateDidEntity>()
            .HasOne(u => u.TransactionEntity)
            .WithMany(s => s.CreateDidEntities)
            .HasForeignKey(e => new { e.TransactionHash, e.BlockHeight, e.BlockHashPrefix })
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<CreateDidEntity>()
            .HasMany(e => e.PrismPublicKeys)
            .WithOne()  // No navigation property back to CreateDidEntity
            .HasForeignKey(p =>  p.CreateDidEntityOperationHash)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<CreateDidEntity>()
            .HasMany(e => e.PrismServices)
            .WithOne()  // No navigation property back to CreateDidEntity
            .HasForeignKey(p =>  p.CreateDidEntityOperationHash)
            .OnDelete(DeleteBehavior.Cascade);
        
    }
}