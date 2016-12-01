using System.Runtime.Serialization;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    [DataContract]
    public class QuerySingleResult<T> : QueryResult, IQuerySingleResult<T>
    {
        public QuerySingleResult()
        { }

        public QuerySingleResult(T data)
        {
            this.Result = data;
        }   

        [DataMember]
        public T Result { get; set; }
    }
}
