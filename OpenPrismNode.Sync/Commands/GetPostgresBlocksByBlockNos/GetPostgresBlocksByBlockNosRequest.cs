using FluentResults;
using MediatR;

namespace OpenPrismNode.Sync.Commands.GetPostgresBlocksByBlockNos
{
    using Core.DbSyncModels;

    public class GetPostgresBlocksByBlockNosRequest : IRequest<Result<List<Block>>>
    {
        public GetPostgresBlocksByBlockNosRequest(int startBlockNo, int count)
        {
            StartBlockNo = startBlockNo;
            Count = count;
        }

        public int StartBlockNo { get; }
        public int Count { get; }
    }
}