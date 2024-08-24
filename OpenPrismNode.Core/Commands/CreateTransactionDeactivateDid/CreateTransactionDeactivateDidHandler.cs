namespace OpenPrismNode.Core.Commands.CreateTransactionDeactivateDid;

using Entities;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Common;

/// <summary>
/// Handler to write a DeacticateDid-PRISM-Operation in the node database
/// </summary>
public class CreateTransactionDeactivateDidHandler : IRequestHandler<CreateTransactionDeactivateDidRequest, Result>
{
    private readonly DataContext _context;
    private readonly ILogger<CreateTransactionDeactivateDidHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateTransactionDeactivateDidHandler(DataContext context, ILogger<CreateTransactionDeactivateDidHandler> logger)
    {
        this._context = context;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(CreateTransactionDeactivateDidRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _context.ChangeTracker.Clear();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            var hasExistingTransaction = await _context.TransactionEntities.AnyAsync(p => p.TransactionHash == request.TransactionHash.Value, cancellationToken: cancellationToken);
            var prefix = BlockEntity.CalculateBlockHashPrefix(request.BlockHash.Value);
            if (!hasExistingTransaction)
            {
                var trans = new TransactionEntity()
                {
                    TransactionHash = request.TransactionHash.Value,
                    Fees = request.Fees,
                    Size = request.Size,
                    Index = request.Index,
                    BlockHeight = request.BlockHeight,
                    BlockHashPrefix = prefix!.Value,
                    Utxos = request.Utxos.DistinctBy(p => (p.Value, p.IsOutgoing, p.Index)).Select(p => new UtxoEntity()
                    {
                        Index = p.Index,
                        Value = p.Value,
                        IsOutgoing = p.IsOutgoing,
                        StakeAddress = p.WalletAddress.StakeAddressString,
                        WalletAddress = p.WalletAddress.WalletAddressString
                    }).ToList(),
                    DeactivateDidEntities = new List<DeactivateDidEntity>()
                    {
                        new DeactivateDidEntity()
                        {
                            OperationHash = request.OperationHash.Value,
                            OperationSequenceNumber = request.OperationSequenceNumber,
                            PreviousOperationHash = request.PreviousOperationHash.Value,
                            Did = PrismEncoding.HexToByteArray(request.Did),
                            SigningKeyId = request.SigningKeyId,
                        }
                    },
                };
                await _context.TransactionEntities.AddAsync(trans, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var prismDeactivateDidEntity =
                    new DeactivateDidEntity()
                    {
                        TransactionHash = request.TransactionHash.Value,
                        OperationHash = request.OperationHash.Value,
                        OperationSequenceNumber = request.OperationSequenceNumber,
                        BlockHeight = request.BlockHeight,
                        BlockHashPrefix = prefix!.Value,
                        PreviousOperationHash = request.PreviousOperationHash.Value,
                        Did = PrismEncoding.HexToByteArray(request.Did),
                        SigningKeyId = request.SigningKeyId,
                    };

                await _context.DeactivateDidEntities.AddAsync(prismDeactivateDidEntity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail($"Invalid operation when saving a DeactivateDid-Operation: blockHeight : '{request.BlockHeight}'. Message: {e.Message} Inner: {e.InnerException?.Message}");
        }
    }
}