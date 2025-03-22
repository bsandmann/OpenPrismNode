namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlocksByBlockNos
{
    using FluentResults;
    using MediatR;
    using OpenPrismNode.Core.DbSyncModels;

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