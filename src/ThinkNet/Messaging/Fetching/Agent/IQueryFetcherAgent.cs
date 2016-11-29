using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Fetching.Agent
{
    public interface IQueryFetcherAgent
    {
        IQueryResult Fetch(IQueryParameter parameter);
    }
}
