using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateWalletAddress;

using Microsoft.Extensions.Logging;
using Services;

public class CreateWalletAddressHandler : IRequestHandler<CreateWalletAddressRequest, Result>
{
    private readonly DataContext _context;
    private readonly IWalletAddressCache _cache;
    private readonly ILogger<CreateWalletAddressHandler> _logger;

    public CreateWalletAddressHandler(DataContext context, IWalletAddressCache cache, ILogger<CreateWalletAddressHandler> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(CreateWalletAddressRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        await _cache.GetOrAddAsync(
            request.WalletAddress,
            async () =>
            {
                var dbWalletAddress = await _context.WalletAddressEntities
                    .FirstOrDefaultAsync(w => w.WalletAddress == request.WalletAddress, cancellationToken);

                if (dbWalletAddress == null)
                {
                    dbWalletAddress = new WalletAddressEntity
                    {
                        WalletAddress = request.WalletAddress
                    };

                    await _context.WalletAddressEntities.AddAsync(dbWalletAddress, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return dbWalletAddress;
            });

        return Result.Ok();
    }
}