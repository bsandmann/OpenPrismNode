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
    public DbSet<LedgerEntity> LedgerEntities { get; set; }
    public DbSet<EpochEntity> EpochEntities { get; set; }
    public DbSet<BlockEntity> BlockEntities { get; set; }
    public DbSet<TransactionEntity> TransactionEntities { get; set; }
    public DbSet<UtxoEntity> UtxoEntities { get; set; }
    public DbSet<StakeAddressEntity> StakeAddressEntities { get; set; }
    public DbSet<WalletAddressEntity> WalletAddressEntities { get; set; }
    public DbSet<CreateDidEntity> CreateDidEntities { get; set; }
    public DbSet<UpdateDidEntity> UpdateDidEntities { get; set; }
    public DbSet<PrismPublicKeyEntity> PrismPublicKeyEntities { get; set; }
    public DbSet<PrismServiceEntity> PrismServiceEntities { get; set; }
    public List<PatchedContextEntity> PatchedContexts { get; set; }
    public DbSet<DeactivateDidEntity> DeactivateDidEntities { get; set; }
    public DbSet<OperationStatusEntity> OperationStatusEntities { get; set; }
    public DbSet<WalletEntity> WalletEntities { get; set; }
    public DbSet<WalletTransactionEntity> WalletTransactionEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LedgerEntity>().HasKey(p => p.Ledger);
        modelBuilder.Entity<EpochEntity>().HasKey(p => new { p.EpochNumber, p.Ledger });
        modelBuilder.Entity<BlockEntity>()
            .HasKey(b => new { b.BlockHeight, b.BlockHashPrefix });
        modelBuilder.Entity<TransactionEntity>()
            .HasKey(t => new { t.TransactionHash, t.BlockHeight, t.BlockHashPrefix });
        modelBuilder.Entity<UtxoEntity>().HasKey(p => p.UtxoEntityId);
        modelBuilder.Entity<CreateDidEntity>().HasKey(p => p.OperationHash);
        modelBuilder.Entity<UpdateDidEntity>().HasKey(p => p.OperationHash);
        modelBuilder.Entity<PrismPublicKeyEntity>().HasKey(p => p.PrismPublicKeyEntityId);
        modelBuilder.Entity<PrismServiceEntity>().HasKey(p => p.PrismServiceEntityId);
        modelBuilder.Entity<PrismPublicKeyEntity>().HasKey(p => p.PrismPublicKeyEntityId);
        modelBuilder.Entity<PrismPublicKeyRemoveEntity>().HasKey(p => p.PrismPublicKeyRemoveEntityId);
        modelBuilder.Entity<PatchedContextEntity>().HasKey(p => p.PatchedContextEntityId);
        modelBuilder.Entity<DeactivateDidEntity>().HasKey(p => p.OperationHash);
        modelBuilder.Entity<WalletTransactionEntity>().HasKey(e => e.WalletTransactionEntityId);
        modelBuilder.Entity<WalletEntity>().HasKey(e => e.WalletEntityId);
        modelBuilder.Entity<OperationStatusEntity>().HasKey(e => e.OperationStatusId);

        modelBuilder.Entity<EpochEntity>()
            .HasOne(p => p.LedgerEntity)
            .WithMany(b => b.Epochs)
            .HasForeignKey(p => p.Ledger)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EpochEntity>()
            .Property(e => e.EpochNumber)
            .ValueGeneratedNever();

        modelBuilder.Entity<BlockEntity>()
            .HasOne(p => p.EpochEntity)
            .WithMany(b => b.BlockEntities)
            .HasForeignKey(p => new { p.EpochNumber, p.Ledger })
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
            .WithOne() // No navigation property back to CreateDidEntity
            .HasForeignKey(p => p.CreateDidEntityOperationHash)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CreateDidEntity>()
            .HasMany(e => e.PrismServices)
            .WithOne() // No navigation property back to CreateDidEntity
            .HasForeignKey(p => p.CreateDidEntityOperationHash)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CreateDidEntity>()
            .HasOne(c => c.PatchedContext)
            .WithOne(p => p.CreateDidEntity)
            .HasForeignKey<PatchedContextEntity>(p => p.CreateDidEntityOperationHash)
            .IsRequired(false);

        modelBuilder.Entity<PatchedContextEntity>()
            .HasOne(p => p.CreateDidEntity)
            .WithOne(c => c.PatchedContext)
            .HasForeignKey<PatchedContextEntity>(p => p.CreateDidEntityOperationHash)
            .IsRequired(false);

        modelBuilder.Entity<UpdateDidEntity>()
            .HasOne(u => u.TransactionEntity)
            .WithMany(s => s.UpdateDidEntities)
            .HasForeignKey(e => new { e.TransactionHash, e.BlockHeight, e.BlockHashPrefix })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UpdateDidEntity>()
            .HasOne(u => u.CreateDidEntity)
            .WithMany(c => c.DidUpdates)
            .HasForeignKey(u => u.Did)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UpdateDidEntity>()
            .HasIndex(u => u.Did);

        modelBuilder.Entity<PrismPublicKeyEntity>()
            .HasOne<UpdateDidEntity>()
            .WithMany(u => u.PrismPublicKeysToAdd)
            .HasForeignKey(p => p.UpdateDidEntityOperationHash)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PrismServiceEntity>()
            .HasOne<UpdateDidEntity>()
            .WithMany(u => u.PrismServices)
            .HasForeignKey(p => p.UpdateDidEntityOperationHash)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PrismPublicKeyRemoveEntity>()
            .HasOne(p => p.UpdateDidEntity)
            .WithMany(u => u.PrismPublicKeysToRemove)
            .HasForeignKey(p => p.UpdateDidOperationHash)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PrismPublicKeyRemoveEntity>()
            .Property(p => p.UpdateDidOperationHash)
            .HasColumnName("UpdateDidEntityOperationHash");

        modelBuilder.Entity<PatchedContextEntity>()
            .HasOne(p => p.UpdateDidEntity)
            .WithMany(u => u.PatchedContexts)
            .HasForeignKey(p => p.UpdateDidEntityOperationHash)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatchedContextEntity>()
            .HasIndex(p => p.ContextListJson)
            .HasMethod("gin");

        modelBuilder.Entity<DeactivateDidEntity>()
            .HasOne(d => d.CreateDidEntity)
            .WithOne(c => c.DidDeactivation)
            .HasForeignKey<DeactivateDidEntity>(d => d.Did)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeactivateDidEntity>()
            .HasOne(d => d.TransactionEntity)
            .WithMany(t => t.DeactivateDidEntities)
            .HasForeignKey(d => new { d.TransactionHash, d.BlockHeight, d.BlockHashPrefix })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeactivateDidEntity>()
            .HasIndex(d => d.Did);

        modelBuilder.Entity<BlockEntity>()
            .HasIndex(b => b.EpochNumber);

        modelBuilder.Entity<TransactionEntity>()
            .HasIndex(t => new { t.BlockHeight, t.BlockHashPrefix });

        modelBuilder.Entity<UtxoEntity>()
            .HasIndex(u => u.StakeAddress);

        modelBuilder.Entity<UtxoEntity>()
            .HasIndex(u => u.WalletAddress);

        modelBuilder.Entity<CreateDidEntity>()
            .HasIndex(c => new { c.TransactionHash, c.BlockHeight, c.BlockHashPrefix });

        modelBuilder.Entity<UpdateDidEntity>()
            .HasIndex(u => new { u.TransactionHash, u.BlockHeight, u.BlockHashPrefix });

        modelBuilder.Entity<PrismPublicKeyEntity>()
            .HasIndex(p => p.CreateDidEntityOperationHash);

        modelBuilder.Entity<PrismPublicKeyEntity>()
            .HasIndex(p => p.UpdateDidEntityOperationHash);

        modelBuilder.Entity<OperationStatusEntity>()
            .HasOne(e => e.WalletTransactionEntity)
            .WithOne(w => w.OperationStatusEntity)
            .HasForeignKey<WalletTransactionEntity>(w => w.OperationStatusId)
            .IsRequired(false);

        modelBuilder.Entity<WalletEntity>()
            .HasMany(e => e.WalletTransactions)
            .WithOne(w => w.Wallet)
            .HasForeignKey(w => w.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WalletTransactionEntity>()
            .HasOne(e => e.Wallet)
            .WithMany(w => w.WalletTransactions)
            .HasForeignKey(e => e.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WalletTransactionEntity>()
            .HasOne(e => e.OperationStatusEntity)
            .WithOne(o => o.WalletTransactionEntity)
            .HasForeignKey<WalletTransactionEntity>(e => e.OperationStatusId)
            .IsRequired(false);

        // modelBuilder.Entity<OperationStatusEntity>()
        //     .HasOne(os => os.CreateDidEntity)
        //     .WithOne(cd => cd.OperationStatus)
        //     .HasForeignKey<OperationStatusEntity>(os => os.OperationHash)
        //     .IsRequired(false);
        //
        // modelBuilder.Entity<OperationStatusEntity>()
        //     .HasOne(os => os.UpdateDidEntity)
        //     .WithOne(ud => ud.OperationStatus)
        //     .HasForeignKey<OperationStatusEntity>(os => os.OperationHash)
        //     .IsRequired(false);
        //
        // modelBuilder.Entity<OperationStatusEntity>()
        //     .HasOne(os => os.DeactivateDidEntity)
        //     .WithOne(dd => dd.OperationStatus)
        //     .HasForeignKey<OperationStatusEntity>(os => os.OperationHash)
        //     .IsRequired(false);
    }
}