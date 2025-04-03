namespace OpenPrismNode.Core.Commands.GetVerificationMethodSecrets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentResults;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using OpenPrismNode.Core.Models;

    public class GetVerificationMethodSecretsHandler
        : IRequestHandler<GetVerificationMethodSecretsRequest, Result<List<VerificationMethodSecret>>>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GetVerificationMethodSecretsHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory 
                ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task<Result<List<VerificationMethodSecret>>> Handle(
            GetVerificationMethodSecretsRequest request, 
            CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                // Attempt to find the OperationStatusEntity associated with the given OperationStatusId
                var operationStatus = await context.OperationStatusEntities
                    // Include VerificationMethodSecrets so we don’t have to query them separately
                    .Include(os => os.VerificationMethodSecrets)
                    .SingleOrDefaultAsync(
                        os => os.OperationStatusId == request.OperationStatusId, 
                        cancellationToken);

                if (operationStatus == null)
                {
                    return Result.Fail<List<VerificationMethodSecret>>(
                        new Error("OperationStatusEntity not found for the provided OperationStatusId."));
                }

                // Convert VerificationMethodSecretEntity objects to the model VerificationMethodSecret
                var secrets = operationStatus.VerificationMethodSecrets
                    .Select(s => new VerificationMethodSecret(
                        prismKeyUsage: s.PrismKeyUsage,
                        keyId: s.KeyId,
                        curve: s.Curve,
                        bytes: s.Bytes,
                        isRemoveOperation: s.IsRemoveOperation,
                        mnemonic: s.Mnemonic
                    ))
                    .ToList();

                return Result.Ok(secrets);
            }
            catch (DbUpdateException dbEx)
            {
                return Result.Fail<List<VerificationMethodSecret>>(
                    new Error("Database error occurred while retrieving verification method secrets.")
                        .CausedBy(dbEx));
            }
            catch (Exception ex)
            {
                return Result.Fail<List<VerificationMethodSecret>>(
                    new Error("An unexpected error occurred while retrieving the verification method secrets.")
                        .CausedBy(ex));
            }
        }
    }
}
