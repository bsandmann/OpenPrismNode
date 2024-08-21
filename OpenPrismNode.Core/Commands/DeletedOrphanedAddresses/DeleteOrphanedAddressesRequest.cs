namespace OpenPrismNode.Core.Commands.DeletedOrphanedAddresses;

using FluentResults;
using MediatR;
using Models;

public class DeleteOrphanedAddressesRequest : IRequest<Result>
{
    public DeleteOrphanedAddressesRequest( LedgerType ledger)
    {
        Ledger = ledger;
    }
    
    /// <summary>
    /// Ledger (testnet, preprod, mainnet)
    /// </summary>
    public LedgerType Ledger { get; }
}