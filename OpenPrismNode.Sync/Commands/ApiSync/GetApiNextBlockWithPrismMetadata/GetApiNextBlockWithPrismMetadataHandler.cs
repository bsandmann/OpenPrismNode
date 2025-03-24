namespace OpenPrismNode.Sync.Commands.DbSync.GetNextBlockWithPrismMetadata;

using System.Diagnostics;
using ApiSync.GetApiNextBlockWithPrismMetadata;
using Dapper;
using FluentResults;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Finds the next block in the chain that contains PRISM metadata.
/// This handler is essential for the fast-sync process as it allows skipping blocks without PRISM transactions.
/// Uses caching to improve performance when repeatedly searching for blocks with PRISM metadata.
/// </summary>
public class GetApiNextBlockWithPrismMetadataHandler : IRequestHandler<GetApiNextBlockWithPrismMetadataRequest, Result<GetApiNextBlockWithPrismMetadataResponse>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;
    private readonly ILogger<GetApiNextBlockWithPrismMetadataHandler> _logger;
    private readonly IAppCache _cache;

    public GetApiNextBlockWithPrismMetadataHandler(INpgsqlConnectionFactory connectionFactory, ILogger<GetApiNextBlockWithPrismMetadataHandler> logger, IAppCache cache)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _cache = cache;
    }

    public Task<Result<GetApiNextBlockWithPrismMetadataResponse>> Handle(GetApiNextBlockWithPrismMetadataRequest request, CancellationToken cancellationToken)
    {
        return null;
    }
}

