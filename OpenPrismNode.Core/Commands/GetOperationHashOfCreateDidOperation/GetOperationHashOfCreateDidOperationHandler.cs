namespace OpenPrismNode.Core.Commands.GetOperationHashOfCreateDidOperation;

using Common;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class GetOperationHashOfCreateDidOperationHandler : IRequestHandler<GetOperationHashOfCreateDidOperationRequest, Result<byte[]>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetOperationHashOfCreateDidOperationHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<byte[]>> Handle(GetOperationHashOfCreateDidOperationRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        var initialDidOperationHash = PrismEncoding.HexToByteArray(request.DidIdentifier);
        var didOperation = await context.CreateDidEntities.FirstOrDefaultAsync(p => p.OperationHash == initialDidOperationHash, cancellationToken: cancellationToken);
        if (didOperation is null)
        {
            return Result.Fail("Unable to find CreateDidOperation with the given DidIdentifier");
        }

        var operationHash = didOperation.OperationHash;
        return Result.Ok(operationHash);
    }
}