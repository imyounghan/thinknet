using System;

namespace ThinkNet.Messaging.Fetching
{
    /// <summary>
    /// 表示一个当找不到查询执行程序的异常
    /// </summary>
    [Serializable]
    public class QueryFetcherNotFoundException : ThinkNetException
    {
        /// <summary>Parameterized constructor.
        /// </summary>
        public QueryFetcherNotFoundException(Type type)
            : base(string.Format("Cannot found the IQueryExecutor<{0}> of implementation.", type.FullName))
        { }
    }
}
