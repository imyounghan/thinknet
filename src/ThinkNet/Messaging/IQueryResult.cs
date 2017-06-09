

namespace ThinkNet.Messaging
{
    public interface IQueryResult : IReplyResult
    {
        object Data { get; }
    }


    public interface IQueryResult<TData> : IReplyResult
    {
        TData Data { get; }
    }
}
