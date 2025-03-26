namespace OpenPrismNode.Core.Commands.CreateLedger;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Entities;

public class CreateLedgerHandler : IRequestHandler<CreateLedgerRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateLedgerHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result> Handle(CreateLedgerRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var existingLedger = await context.LedgerEntities.FirstOrDefaultAsync(p => p.Ledger == request.LedgerType, cancellationToken: cancellationToken);
        var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow,DateTimeKind.Unspecified);
        if (existingLedger is null)
        {
            var ledger = new LedgerEntity()
            {
                Ledger = request.LedgerType,
                LastSynced = dateTimeNow
            };

            await context.AddAsync(ledger, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }
}