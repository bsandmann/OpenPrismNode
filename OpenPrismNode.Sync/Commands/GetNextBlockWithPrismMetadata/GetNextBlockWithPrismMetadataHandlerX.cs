// using Dapper;
// using FluentResults;
// using MediatR;
// using Microsoft.Extensions.Logging;
// using OpenPrismNode.Sync.Services;
//
// public class GetNextBlockWithPrismMetadataHandlerX : IRequestHandler<GetNextBlockWithPrismMetadataRequest, Result<GetNextBlockWithPrismMetadataResponse>>
// {
//     private readonly INpgsqlConnectionFactory _connectionFactory;
//     private readonly ILogger<GetNextBlockWithPrismMetadataHandlerX> _logger;
//
//     public GetNextBlockWithPrismMetadataHandlerX(INpgsqlConnectionFactory connectionFactory, ILogger<GetNextBlockWithPrismMetadataHandlerX> logger)
//     {
//         _connectionFactory = connectionFactory;
//         _logger = logger;
//     }
//
//     public async Task<Result<GetNextBlockWithPrismMetadataResponse>> Handle(GetNextBlockWithPrismMetadataRequest request, CancellationToken cancellationToken)
//     {
//         await using var connection = _connectionFactory.CreateConnection();
//
//         const int batchSize = 1_000; // Adjust this based on your database performance
//         int currentBlockHeight = request.StartBlockHeight;
//
//         while (true)
//         {
//             _logger.LogInformation($"Checking for block with PRISM-metadata between {currentBlockHeight} and {currentBlockHeight + batchSize}");
//             const string commandText = @"
//                 SELECT b.block_no, b.epoch_no
//                 FROM public.block b
//                 WHERE b.block_no > @StartBlockHeight
//                   AND b.block_no <= @EndBlockHeight
//                   AND EXISTS (
//                     SELECT 1
//                     FROM public.tx t
//                     JOIN public.tx_metadata m ON t.id = m.tx_id
//                     WHERE t.block_id = b.id AND m.key = @MetadataKey
//                   )
//                 ORDER BY b.block_no ASC
//                 LIMIT 1";
//
//             var parameters = new
//             {
//                 StartBlockHeight = currentBlockHeight,
//                 EndBlockHeight = currentBlockHeight + batchSize,
//                 request.MetadataKey
//             };
//
//             var result = await connection.QueryFirstOrDefaultAsync<(int? BlockNumber, int? EpochNumber)>(commandText, parameters);
//
//             if (result.BlockNumber.HasValue)
//             {
//                 _logger.LogInformation($"Found next PRISM block at {result.BlockNumber.Value}");
//                 return Result.Ok(new GetNextBlockWithPrismMetadataResponse
//                 {
//                     BlockHeight = result.BlockNumber.Value,
//                     EpochNumber = result.EpochNumber.Value
//                 });
//             }
//
//             currentBlockHeight += batchSize;
//             
//             if (currentBlockHeight > request.MaxBlockHeight) 
//             {
//                 return Result.Ok(new GetNextBlockWithPrismMetadataResponse());
//             }
//         }
//     }
// }