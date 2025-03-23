namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiPaymentDataFromTransaction;

using Core.DbSyncModels;
using Core.Models;
using FluentResults;
using GetApiAddressDetails;
using GetApiTransactionUtxos;
using MediatR;

public class GetApiPaymentDataFromTransactionHandler : IRequestHandler<GetApiPaymentDataFromTransactionRequest, Result<Payment>>
{
    private readonly IMediator _mediator;

    public GetApiPaymentDataFromTransactionHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<Payment>> Handle(GetApiPaymentDataFromTransactionRequest request, CancellationToken cancellationToken)
    {
        var tranacactionUtxosResult = await _mediator.Send(new GetApiTransactionUtxosRequest(request.TxHash), cancellationToken);
        if (tranacactionUtxosResult.IsFailed)
        {
            return Result.Fail<Payment>(tranacactionUtxosResult.Errors.First().Message);
        }

        var walletAddressesInput = tranacactionUtxosResult.Value.Inputs.Select(p => p.Address);
        var walletAddressesOutput = tranacactionUtxosResult.Value.Outputs.Select(p => p.Address);
        var walletAddresses = walletAddressesInput.Concat(walletAddressesOutput).Distinct().ToList();

        var addressDict = new Dictionary<string, string>();
        foreach (var walletAddress in walletAddresses)
        {
            var walletAddressResult = await _mediator.Send(new GetApiAddressDetailsRequest(walletAddress), cancellationToken);
            if (walletAddressResult.IsFailed)
            {
                return Result.Fail<Payment>(walletAddressResult.Errors.FirstOrDefault()?.Message);
            }

            addressDict.Add(walletAddress, walletAddressResult.Value.StakeAddress);
        }

        var incommingUtxos = new List<Utxo>();
        foreach (var utxoInput in tranacactionUtxosResult.Value.Inputs)
        {
            incommingUtxos.Add(new Utxo()
            {
                Index = utxoInput.OutputIndex,
                Value = utxoInput.Amount.Where(p => p.Unit.Equals("lovelace")).Sum(q => long.TryParse(q.Quantity, out var am) ? am : 0),
                WalletAddress = new WalletAddress()
                {
                    WalletAddressString = utxoInput.Address,
                    StakeAddressString = addressDict.TryGetValue(utxoInput.Address, out string? s) ? s : string.Empty
                }
            });
        }

        var outgoingUtxos = new List<Utxo>();
        foreach (var utxoOutput in tranacactionUtxosResult.Value.Outputs)
        {
            outgoingUtxos.Add(new Utxo()
            {
                Index = utxoOutput.OutputIndex,
                Value = utxoOutput.Amount.Where(p => p.Unit.Equals("lovelace")).Sum(q => long.TryParse(q.Quantity, out var am) ? am : 0),
                WalletAddress = new WalletAddress()
                {
                    WalletAddressString = utxoOutput.Address,
                    StakeAddressString = addressDict.TryGetValue(utxoOutput.Address, out string? s) ? s : string.Empty
                }
            });
        }

        return Result.Ok(new Payment()
        {
            Outgoing = outgoingUtxos,
            Incoming = incommingUtxos
        });
    }
}