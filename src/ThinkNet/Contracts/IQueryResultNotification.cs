
namespace ThinkNet.Contracts
{
    public interface IQueryResultNotification
    {
        void Notify(string queryId, IQueryResult queryResult);
    }
}
