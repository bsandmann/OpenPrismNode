using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateStakeAddress;

using CreateWalletAddress;
using Microsoft.Extensions.Logging;
using Npgsql;
using Services;

public class CreateStakeAddressHandler : IRequestHandler<CreateStakeAddressRequest, Result>
{
    private readonly DataContext _context;
    private readonly IStakeAddressCache _cache;
    private readonly ILogger<CreateStakeAddressHandler> _logger;

    public CreateStakeAddressHandler(DataContext context, IStakeAddressCache cache, ILogger<CreateStakeAddressHandler> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(CreateStakeAddressRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        await _cache.GetOrAddAsync(
            request.StakeAddress,
            async () =>
            {
                var dbStakeAddress = await _context.StakeAddressEntities
                    .FirstOrDefaultAsync(w => w.StakeAddress == request.StakeAddress, cancellationToken);

                if (dbStakeAddress == null)
                {
                    dbStakeAddress = new StakeAddressEntity()
                    {
                        StakeAddress = request.StakeAddress
                    };

                    await _context.StakeAddressEntities.AddAsync(dbStakeAddress, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return dbStakeAddress;
            });

        return Result.Ok();
    }
}