using System;

namespace ThinkNet.Messaging.Fetching
{
    /// <summary>
    /// 表示一个存在多个消息处理程序的异常
    /// </summary>
    public class QueryFetcherTooManyException : ThinkNetException
    {
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public QueryFetcherTooManyException(Type type)
            : base(string.Format("Found more than one implementations of IQueryExecutor<{0}>.", type.FullName)) 
        { }
    }
}
