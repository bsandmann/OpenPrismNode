using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core.Entities; // Make sure this points to your entity namespace

namespace OpenPrismNode.Core.Commands.Registrar.CreateVerificationMethodSecret
{
    public class CreateVerificationMethodSecretHandler : IRequestHandler<CreateVerificationMethodSecretRequest, Result>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public CreateVerificationMethodSecretHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task<Result> Handle(CreateVerificationMethodSecretRequest request, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                // Create the new VerificationMethodSecretEntity
                var newSecretEntity = new VerificationMethodSecretEntity
                {
                    // Set the Foreign Key
                    OperationStatusEntityId = request.ValueOperationStatusEntityId,

                    // Map properties from the request model
                    PrismKeyUsage = request.VerificationMethodSecret.PrismKeyUsage,
                    KeyId = request.VerificationMethodSecret.KeyId,
                    Curve = request.VerificationMethodSecret.Curve,
                    Bytes = request.VerificationMethodSecret.Bytes,
                    IsRemoveOperation = request.VerificationMethodSecret.IsRemoveOperation,
                    Mnemonic = request.VerificationMethodSecret.Mnemonic
                };

                await context.VerificationMethodSecrets.AddAsync(newSecretEntity, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return Result.Ok();
            }
            catch (DbUpdateException dbEx) // Catch specific EF Core update exceptions
            {
                return Result.Fail(new Error($"Database error occurred while saving verification method secret. See inner exception for details.").CausedBy(dbEx));
            }
            catch (Exception ex) // Catch general exceptions
            {
                return Result.Fail(new Error("An unexpected error occurred while creating the verification method secret.").CausedBy(ex));
            }
        }
    }
}