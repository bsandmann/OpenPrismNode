namespace OpenPrismNode.Core.Commands.GetOperationLedgerTime;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Common;

public class GetOperationLedgerTimeHandler : IRequestHandler<GetOperationLedgerTimeRequest, Result<GetOperationLedgerTimeResponse>>
{
    private readonly DataContext _context;

    public GetOperationLedgerTimeHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<GetOperationLedgerTimeResponse>> Handle(GetOperationLedgerTimeRequest request, CancellationToken cancellationToken)
    {
        // we are looking for a specifc operationhash - and 
        byte[] versionIdBytes;
        try
        {
            versionIdBytes = PrismEncoding.HexToByteArray(request.VersionId);
        }
        catch (Exception e)
        {
            return Result.Fail("Invalid versionId. Expected base64 string to be converted to byte array");
        }

        var createEntity = await _context.CreateDidEntities
            .Select(p => new
            {
                p.BlockHeight,
                p.OperationHash,
                p.OperationSequenceNumber,
                p.TransactionEntity.Index,
                p.TransactionEntity.BlockEntity.Ledger
            })
            .FirstOrDefaultAsync(p => p.OperationHash == versionIdBytes && p.Ledger == request.Ledger, cancellationToken);
        if (createEntity is not null)
        {
            return Result.Ok(new GetOperationLedgerTimeResponse()
            {
                LedgerTimeBlockHeight = createEntity.BlockHeight,
                LedgerTimeBlockSequence = createEntity.Index,
                LedgerTimeOperationSequence = createEntity.OperationSequenceNumber
            });
        }

        var updateEntity = await _context.UpdateDidEntities
            .Select(p => new
            {
                p.BlockHeight,
                p.OperationHash,
                p.OperationSequenceNumber,
                p.TransactionEntity.Index,
                p.TransactionEntity.BlockEntity.Ledger
            })
            .FirstOrDefaultAsync(p => p.OperationHash == versionIdBytes && p.Ledger == request.Ledger, cancellationToken);
        if (updateEntity is not null)
        {
            return Result.Ok(new GetOperationLedgerTimeResponse()
            {
                LedgerTimeBlockHeight = updateEntity.BlockHeight,
                LedgerTimeBlockSequence = updateEntity.Index,
                LedgerTimeOperationSequence = updateEntity.OperationSequenceNumber
            });
        }

        var deactivateEntity = await _context.DeactivateDidEntities
            .Select(p => new
            {
                p.BlockHeight,
                p.OperationHash,
                p.OperationSequenceNumber,
                p.TransactionEntity.Index,
                p.TransactionEntity.BlockEntity.Ledger
            })
            .FirstOrDefaultAsync(p => p.OperationHash == versionIdBytes && p.Ledger == request.Ledger, cancellationToken);

        if (deactivateEntity is not null)
        {
            return Result.Ok(new GetOperationLedgerTimeResponse()
            {
                LedgerTimeBlockHeight = deactivateEntity.BlockHeight,
                LedgerTimeBlockSequence = deactivateEntity.Index,
                LedgerTimeOperationSequence = deactivateEntity.OperationSequenceNumber
            });
        }

        return Result.Fail($"Operation not found for requested versionId '{request.VersionId}' on ledger '{request.Ledger}' not found");
    }
}