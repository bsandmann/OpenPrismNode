using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.GetDidList
{
    public class GetDidListHandler : IRequestHandler<GetDidListRequest, Result<List<DidListResponseItem>>>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GetDidListHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<Result<List<DidListResponseItem>>> Handle(GetDidListRequest request, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var didList = await context.Set<CreateDidEntity>()
                .Where(d => d.TransactionEntity.BlockEntity.EpochEntity.Ledger == request.Ledger)
                .Select(d => new DidListResponseItem
                {
                    Did = "did:prism:" + PrismEncoding.ByteArrayToHex(d.OperationHash),
                    Time = DateTime.SpecifyKind(d.TransactionEntity.BlockEntity.TimeUtc, DateTimeKind.Utc),
                    BlockHeight = d.TransactionEntity.BlockHeight
                })
                .ToListAsync(cancellationToken);

            return Result.Ok(didList);
        }
    }
}