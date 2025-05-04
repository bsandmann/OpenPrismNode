using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;
using System.Collections.Generic;

namespace OpenPrismNode.Core.Commands.GetDidList
{
    public class GetDidListRequest : IRequest<Result<List<DidListResponseItem>>>
    {
        /// <summary>
        /// The ledger to get DIDs from
        /// </summary>
        public LedgerType Ledger { get; set; }
    }
}