namespace OpenPrismNode.Core.Commands.CreateLedger;

using CreateLedger;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Entities;

public class CreateLedgerHandler : IRequestHandler<CreateLedgerRequest, Result>
{
    private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateLedgerHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(CreateLedgerRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        var existingLedger = await _context.LedgerEntities.FirstOrDefaultAsync(p => p.Ledger == request.LedgerType, cancellationToken: cancellationToken);
        var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow,DateTimeKind.Unspecified);
        if (existingLedger is null)
        {
            var ledger = new LedgerEntity()
            {
                Ledger = request.LedgerType,
                LastSynced = dateTimeNow
            };

            await _context.AddAsync(ledger, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }
}