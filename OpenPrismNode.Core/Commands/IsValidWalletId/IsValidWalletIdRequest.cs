using System.Text.RegularExpressions;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace OpenPrismNode.Core.Commands.IsValidWalletId
{
    /// <summary>
    /// Request object for checking if a provided wallet-id is valid (regex + existence in DB).
    /// </summary>
    public class IsValidWalletIdRequest : IRequest<Result<bool>>
    {
        public IsValidWalletIdRequest(string walletId)
        {
            WalletId = walletId;
        }

        public string WalletId { get; }
    }

}
