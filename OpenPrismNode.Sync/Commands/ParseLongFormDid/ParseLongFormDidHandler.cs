namespace OpenPrismNode.Sync.Commands.ParseLongFormDid;

using Core.Models;
using FluentResults;
using Google.Protobuf;
using MediatR;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using ParseTransaction;

public class ParseLongFormDidHandler : IRequestHandler<ParseLongFormDidRequest, Result<InternalDidDocument>>
{
    private readonly ISha256Service _sha256Service;
    private readonly ICryptoService _cryptoService;

    public ParseLongFormDidHandler(ISha256Service sha256Service, ICryptoService cryptoService)
    {
        _sha256Service = sha256Service;
        _cryptoService = cryptoService;
    }

    public async Task<Result<InternalDidDocument>> Handle(ParseLongFormDidRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ParsedDidUrl.MethodSpecificId))
        {
            return Result.Fail("MethodSpecificId is required");
        }

        if (string.IsNullOrWhiteSpace(request.ParsedDidUrl.PrismLongForm))
        {
            return Result.Fail("Longform-DID is required");
        }

        var longFormBytes = PrismEncoding.Base64ToByteArray(request.ParsedDidUrl.PrismLongForm);
        var atalaOperation = AtalaOperation.Parser.ParseFrom(longFormBytes);

        if (atalaOperation.OperationCase != AtalaOperation.OperationOneofCase.CreateDid)
        {
            return Result.Fail("The provided longform-DID is not a CreateDid operation");
        }

        var longFormbytes = _sha256Service.HashData(longFormBytes);
        var longFormHashAsHex = PrismEncoding.ByteArrayToHex(longFormbytes);

        if (!longFormHashAsHex.Equals(request.ParsedDidUrl.MethodSpecificId))
        {
            return Result.Fail("The provided longform-DID does not match the did-identifier");
        }

        var parseTransactionHandler = new ParseTransactionHandler(null!, _sha256Service, _cryptoService, null!);
        var stubSignedAtalaOperation = new SignedAtalaOperation()
        {
            Operation = atalaOperation, Signature = ByteString.Empty, SignedWith = string.Empty
        };
        var parseResult = parseTransactionHandler.ParseCreateDidOperation(stubSignedAtalaOperation, 0);
        if (parseResult.IsFailed)
        {
            return parseResult.ToResult();
        }
        
        var internalDidDocument = parseResult.Value.AsCreateDid().didDocument;
        internalDidDocument.VersionId = request.ParsedDidUrl.MethodSpecificId;
        return Result.Ok(internalDidDocument);
    }
}