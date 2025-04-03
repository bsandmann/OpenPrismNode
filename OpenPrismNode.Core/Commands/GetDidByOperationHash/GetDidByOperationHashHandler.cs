namespace OpenPrismNode.Core.Commands.GetDidByOperationHash
{
    using Common;
    using FluentResults;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using OpenPrismNode.Core.Models;

    public class GetDidByOperationHashHandler
        : IRequestHandler<GetDidByOperationHashRequest, Result<string>>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GetDidByOperationHashHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<Result<string>> Handle(
            GetDidByOperationHashRequest request,
            CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                // 2) Based on the OperationType, fetch the relevant entity
                byte[]? didBytes = null;
                switch (request.OperationType)
                {
                    case OperationTypeEnum.CreateDid:
                    {
                        // OperationHash is the "primary key" in CreateDidEntity
                        var createDidEntity = await context.CreateDidEntities
                            .SingleOrDefaultAsync(
                                c => c.OperationHash == request.OperationHash,
                                cancellationToken);

                        if (createDidEntity == null)
                        {
                            return Result.Fail(
                                $"No CreateDidEntity found for OperationHash {PrismEncoding.ByteArrayToHex(request.OperationHash)}");
                        }

                        // In a CreateDidEntity, the "DID" we want is actually the OperationHash itself
                        didBytes = createDidEntity.OperationHash;
                        break;
                    }

                    case OperationTypeEnum.UpdateDid:
                    {
                        // OperationHash is the "primary key" in UpdateDidEntity
                        var updateDidEntity = await context.UpdateDidEntities
                            .SingleOrDefaultAsync(
                                u => u.OperationHash == request.OperationHash,
                                cancellationToken);

                        if (updateDidEntity == null)
                        {
                            return Result.Fail(
                                $"No UpdateDidEntity found for OperationHash {PrismEncoding.ByteArrayToHex(request.OperationHash)}");
                        }

                        // In an UpdateDidEntity, the DID is in the "Did" property
                        didBytes = updateDidEntity.Did;
                        break;
                    }

                    case OperationTypeEnum.DeactivateDid:
                    {
                        // OperationHash is the "primary key" in DeactivateDidEntity
                        var deactivateDidEntity = await context.DeactivateDidEntities
                            .SingleOrDefaultAsync(
                                d => d.OperationHash == request.OperationHash,
                                cancellationToken);

                        if (deactivateDidEntity == null)
                        {
                            return Result.Fail(
                                $"No DeactivateDidEntity found for OperationHash {PrismEncoding.ByteArrayToHex(request.OperationHash)}");
                        }

                        // In a DeactivateDidEntity, the DID is in the "Did" property
                        didBytes = deactivateDidEntity.Did;
                        break;
                    }

                    default:
                    {
                        return Result.Fail(
                            $"OperationType '{request.OperationType}' is not supported for retrieving a DID.");
                    }
                }

                if (didBytes == null || didBytes.Length == 0)
                {
                    return Result.Fail(
                        "DID bytes not found or empty.");
                }

                var didHex = PrismEncoding.ByteArrayToHex(didBytes);

                return Result.Ok(didHex);
            }
            catch (DbUpdateException dbEx)
            {
                return Result.Fail(
                    new Error("Database error occurred while retrieving DID.")
                        .CausedBy(dbEx));
            }
            catch (Exception ex)
            {
                return Result.Fail(
                    new Error("An unexpected error occurred while retrieving the DID.")
                        .CausedBy(ex));
            }
        }
    }
}