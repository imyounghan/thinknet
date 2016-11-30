using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    public class QuerySingleResult<T> : QueryResult, IQuerySingleResult<T>
    {
        public QuerySingleResult()
        { }

        public QuerySingleResult(T data)
        {
            this.Result = data;
        }   

        public T Result { get; set; }
    }
}
