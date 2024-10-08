﻿namespace OpenPrismNode.Core.Commands.EncodeTransaction;

using System.Text.Json;
using FluentResults;
using Google.Protobuf;
using MediatR;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Grpc.Models;

/// <summary>
/// Takes a list of Signed Atala Operations and coverts it into a json which can be written to the blockchain 
/// </summary>
public class EncodeTransactionHandler : IRequestHandler<EncodeTransactionRequest, Result<TransactionModel>>
{
    public async Task<Result<TransactionModel>> Handle(EncodeTransactionRequest request, CancellationToken cancellationToken)
    {
        var atalaBLock = new AtalaBlock();
        if (!request.SignedAtalaOperations.Any())
        {
            return Result.Fail("No operations to encode.");
        }
        atalaBLock.Operations.AddRange(request.SignedAtalaOperations);

        var atalaObject = new AtalaObject();
        atalaObject.BlockContent = atalaBLock;

        var byteArray = atalaObject.ToByteArray();
        var completeHex = PrismEncoding.ByteArrayToHex(byteArray);
        var rest = completeHex;
        var splitedHex = new List<string>();
        do
        {
            var length = rest.Length > 128 ? 128 : rest.Length;
            splitedHex.Add(String.Concat("0x", rest.Substring(0, length)));
            rest = rest.Substring(length);
        } while (!String.IsNullOrWhiteSpace(rest));

        return new TransactionModel()
        {
            Content = splitedHex.ToList(),
            Version = 1
        };
    }
}