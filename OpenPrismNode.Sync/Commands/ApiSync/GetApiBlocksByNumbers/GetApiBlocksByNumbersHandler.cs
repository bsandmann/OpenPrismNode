using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync;
using OpenPrismNode.Sync.Implementations.Blockfrost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlocksByNumbers
{
    using GetApiBlockByNumber;

    public class GetApiBlocksByNumbersHandler
        : IRequestHandler<GetApiBlocksByNumbersRequest, Result<List<Block>>>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GetApiBlocksByNumbersHandler> _logger;
        private readonly AppSettings _appSettings;
        private readonly IMediator _mediator;

        public GetApiBlocksByNumbersHandler(
            IHttpClientFactory httpClientFactory,
            ILogger<GetApiBlocksByNumbersHandler> logger,
            IMediator mediator,
            IOptions<AppSettings> appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
            _mediator = mediator;
        }

        public async Task<Result<List<Block>>> Handle(
            GetApiBlocksByNumbersRequest request, 
            CancellationToken cancellationToken)
        {
            try
            {
                if (request.Count < 1)
                {
                    return Result.Fail<List<Block>>("Count must be at least 1.");
                }

                _logger.LogDebug("Fetching {Count} blocks starting at block #{FirstBlockNo}",
                                 request.Count, request.FirstBlockNo);

                var client = _httpClientFactory.CreateClient();
                var baseUrl = _appSettings.Blockfrost.BaseUrl;
                var apiKey = _appSettings.Blockfrost.ApiKey;

                // 1) Bulk fetch in pages, store in dictionary to remove duplicates.
                var blocksByNumber = new Dictionary<long, Block>();

                int remaining = request.Count;
                int page = 1;
                long blockToUseForNextCall = request.FirstBlockNo - 1;
                // so the first block returned from "/next" is request.FirstBlockNo

                while (remaining > 0)
                {
                    // API's max "count" query param is 100
                    int pageSize = Math.Min(100, remaining);

                    var nextBlocksRequest = BlockfrostHelper.CreateBlockfrostRequest(
                        baseUrl,
                        apiKey,
                        $"blocks/{blockToUseForNextCall}/next?count={pageSize}&page={page}");

                    var nextBlocksResult = await BlockfrostHelper
                        .SendBlockfrostRequestAsync<List<BlockfrostBlockResponse>>(
                            client, nextBlocksRequest, _logger, cancellationToken);

                    if (nextBlocksResult.IsFailed)
                    {
                        return Result.Fail<List<Block>>(nextBlocksResult.Errors);
                    }

                    var nextBlocksList = nextBlocksResult.Value;

                    // Add them to our dictionary
                    foreach (var bfBlock in nextBlocksList.Where(p=>p.Height != 0))
                    {
                        var mappedBlock = BlockfrostBlockMapper.MapToBlock(bfBlock);
                        // Insert only if not already present
                        if (!blocksByNumber.ContainsKey(mappedBlock.block_no))
                        {
                            blocksByNumber[mappedBlock.block_no] = mappedBlock;
                        }
                    }

                    remaining -= pageSize;
                    page++;

                    // If we got fewer than pageSize, we reached chain tip
                    if (nextBlocksList.Count < pageSize)
                    {
                        break;
                    }
                }

                // 2) Check for missing blocks. 
                //    Instead of fetching them one by one, first try to fetch 
                //    consecutive sequences in bulk, then fallback individually if needed.
                var missingBlocks = new List<long>();
                for (long blockNo = request.FirstBlockNo; 
                         blockNo < request.FirstBlockNo + request.Count; 
                         blockNo++)
                {
                    if (!blocksByNumber.ContainsKey(blockNo))
                    {
                        missingBlocks.Add(blockNo);
                    }
                }

                if (missingBlocks.Any())
                {
                    var consecutiveGroups = GroupConsecutiveNumbers(missingBlocks);

                    foreach (var group in consecutiveGroups)
                    {
                        long groupStart = group.First();
                        long groupEnd   = group.Last();
                        int groupSize   = (int)(groupEnd - groupStart + 1);

                        // Try once more in bulk (e.g., by calling the same command again).
                        // You could also inline the "blocks/{block}/next" approach here if you prefer.
                        _logger.LogDebug(
                            "Attempting consecutive bulk fetch for missing blocks [{Start}..{End}]",
                            groupStart, groupEnd);

                        var subRequest = new GetApiBlocksByNumbersRequest((int)groupStart, groupSize);
                        var bulkResult = await _mediator.Send(subRequest, cancellationToken);

                        if (bulkResult.IsFailed || bulkResult.Value.Count != groupSize)
                        {
                            // Fallback: fetch individually the ones still missing
                            _logger.LogWarning(
                                "Consecutive bulk fetch incomplete for blocks [{Start}..{End}] - falling back to single-block fetch",
                                groupStart, groupEnd);

                            foreach (var blockNum in group)
                            {
                                if (!blocksByNumber.ContainsKey(blockNum))
                                {
                                    var singleBlockResult = await _mediator.Send(
                                        new GetApiBlockByNumberRequest((int)blockNum),
                                        cancellationToken);

                                    if (singleBlockResult.IsSuccess)
                                    {
                                        blocksByNumber[blockNum] = singleBlockResult.Value;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If our second bulk fetch succeeded, store them
                            foreach (var b in bulkResult.Value)
                            {
                                if (!blocksByNumber.ContainsKey(b.block_no))
                                {
                                    blocksByNumber[b.block_no] = b;
                                }
                            }
                        }
                    }
                }

                // 3) Build final list and ensure sequential continuity
                var finalBlocks = blocksByNumber
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => kvp.Value)
                    .ToList();

                if (finalBlocks.Count != request.Count)
                {
                    return Result.Fail<List<Block>>(
                        $"Expected {request.Count} blocks but got {finalBlocks.Count}.");
                }

                // Check each block is consecutive (no random jumps)
                for (int i = 1; i < finalBlocks.Count; i++)
                {
                    var expectedNext = finalBlocks[i - 1].block_no + 1;
                    if (finalBlocks[i].block_no != expectedNext)
                    {
                        return Result.Fail<List<Block>>(
                            $"Gap between #{finalBlocks[i - 1].block_no} and #{finalBlocks[i].block_no}");
                    }
                }

                _logger.LogDebug("Successfully fetched {Count} blocks from #{Start} to #{End}",
                                 finalBlocks.Count,
                                 finalBlocks.First().block_no,
                                 finalBlocks.Last().block_no);

                ValidateFetchedBlocks(finalBlocks, request.FirstBlockNo);

                return Result.Ok(finalBlocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error while fetching blocks starting at #{FirstBlockNo}",
                    request.FirstBlockNo);
                return Result.Fail<List<Block>>($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Throws an exception if block_no in <paramref name="blocks"/> are not unique
        /// or not strictly sequential starting from <paramref name="firstBlockNo"/>.
        /// </summary>
        private static void ValidateFetchedBlocks(IReadOnlyList<Block> blocks, long firstBlockNo)
        {
            if (blocks == null || blocks.Count == 0)
            {
                throw new InvalidOperationException("No blocks were fetched.");
            }

            // 1) Check uniqueness
            var duplicates = blocks
                .GroupBy(b => b.block_no)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                var duplicatesStr = string.Join(", ", duplicates);
                throw new InvalidOperationException(
                    $"Duplicate block_no values found in the fetched blocks. Duplicates: [{duplicatesStr}]"
                );
            }

            // 2) Check strict sequential order
            for (int i = 0; i < blocks.Count; i++)
            {
                long expectedNo = firstBlockNo + i;
                if (blocks[i].block_no != expectedNo)
                {
                    throw new InvalidOperationException(
                        $"Blocks are not sequential. Expected block_no={expectedNo}, " +
                        $"but found {blocks[i].block_no} at index {i}."
                    );
                }
            }
        }

        /// <summary>
        /// Groups a list of block numbers into lists of consecutive runs.
        /// e.g., [1,2,3,5,6,9] => [[1,2,3],[5,6],[9]]
        /// </summary>
        private static List<List<long>> GroupConsecutiveNumbers(List<long> numbers)
        {
            var result = new List<List<long>>();
            if (numbers == null || numbers.Count == 0) return result;

            numbers.Sort();
            var currentGroup = new List<long> { numbers[0] };

            for (int i = 1; i < numbers.Count; i++)
            {
                // If current number is exactly +1 from previous, continue the group
                if (numbers[i] == numbers[i - 1] + 1)
                {
                    currentGroup.Add(numbers[i]);
                }
                else
                {
                    // Start a new group
                    result.Add(currentGroup);
                    currentGroup = new List<long> { numbers[i] };
                }
            }

            // Add the final group
            result.Add(currentGroup);
            return result;
        }
    }
}
