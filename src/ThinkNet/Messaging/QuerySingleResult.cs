using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    public class QuerySingleResult<T> : QueryResult, IQuerySingleResult<T>
    {
        public QuerySingleResult()
        { }

        public QuerySingleResult(T data)
            : base(QueryStatus.Success, null)
        {
            this.Data = data;
        }   

        public T Data { get; set; }
    }
}
