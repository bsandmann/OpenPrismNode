using System.Text.RegularExpressions;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace OpenPrismNode.Core.Commands.IsValidWalletId
{
    /// <summary>
    /// Handler that checks if a wallet-id is valid and exists in the database.
    /// </summary>
    public class IsValidWalletIdHandler : IRequestHandler<IsValidWalletIdRequest, Result<bool>>
    {
        private readonly DataContext _context;

        public IsValidWalletIdHandler(DataContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> Handle(IsValidWalletIdRequest request, CancellationToken cancellationToken)
        {
            // 1) Regex check (early return on failure).
            if (!IsValidWalletId(request.WalletId))
            {
                return Result.Fail("Invalid walletId format.");
            }

            try
            {
                // 2) Check existence in DB.
                var exists = await _context.WalletEntities
                    .AnyAsync(w => w.WalletId == request.WalletId, cancellationToken);

                // If it exists -> true, otherwise false. 
                return Result.Ok(exists);
            }
            catch (Exception ex)
            {
                // Catch any DB-related errors and convert to Result.Fail
                return Result.Fail($"Database error while checking wallet ID: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the walletId is a valid 40-hex string using a regex pattern.
        /// </summary>
        private static bool IsValidWalletId(string walletId)
        {
            if (string.IsNullOrWhiteSpace(walletId))
                return false;

            // \A and \z ensure the match spans the entire string.
            // [0-9a-f]{40} matches exactly 40 hex characters.
            return Regex.IsMatch(walletId, @"\A[0-9a-f]{40}\z", RegexOptions.IgnoreCase);
        }
    }
}
