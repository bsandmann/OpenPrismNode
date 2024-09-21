namespace OpenPrismNode.Sync.Commands.DecodeTransaction;

using System.Text.Json;
using FluentResults;
using MediatR;
using Models;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Grpc.Models;

/// <summary>
/// Takes the raw transaction data as json and decodes it into a list of SignedAtalaOperations
/// </summary>
public class DecodeTransactionHandler : IRequestHandler<DecodeTransactionRequest, Result<List<SignedAtalaOperation>>>
{
    public async Task<Result<List<SignedAtalaOperation>>> Handle(DecodeTransactionRequest request, CancellationToken cancellationToken)
    {
        TransactionModel? transactionModel;
        try
        {
            transactionModel = JsonSerializer.Deserialize<TransactionModel>(request.Json);
            if (transactionModel is null || transactionModel.Content is null)
            {
                return Result.Fail(ParserErrors.InvalidDeserializationResult);
            }
        }
        catch (Exception)
        {
            return Result.Fail(ParserErrors.UnableToDeserializeBlockchainMetadata);
        }

        var hexList = transactionModel.Content.Select(p => p.Substring(2).Trim());
        var hexCombined = string.Join(string.Empty, hexList);
        var version = transactionModel.Version;
        if (version != 1)
        {
            return Result.Fail(ParserErrors.UnsupportedPrismBlockVersion);
        }

        AtalaObject? parsedAtalaObject = null;
        try
        {
            parsedAtalaObject = AtalaObject.Parser.ParseFrom(PrismEncoding.HexToByteString(hexCombined));
            if (parsedAtalaObject is null)
            {
                return Result.Fail(ParserErrors.ParsingFailedInvalidForm);
            }
        }
        catch (Exception)
        {
            return Result.Fail(ParserErrors.ParsingFailedInvalidForm);
        }

        var blockContent = parsedAtalaObject.BlockContent;
        if (blockContent is null)
        {
            // This could be an old PRISM-transaction on the blockchain which we're unable to parse
            return Result.Fail(ParserErrors.UnsupportedPrismOperationLikleyOldFormat);
        }

        return Result.Ok(blockContent.Operations.ToList());
    }
}