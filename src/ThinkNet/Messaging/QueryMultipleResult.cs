using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    public class QueryMultipleResult<T> : QueryResult, IQueryMultipleResult<T>
    {
        /// <summary>
        /// 空数据
        /// </summary>
        public static readonly QueryMultipleResult<T> Empty = new QueryMultipleResult<T>();

        private readonly IEnumerable<T> datas;

        public QueryMultipleResult()
        {
            this.datas = Enumerable.Empty<T>();
        }

        public QueryMultipleResult(IEnumerable<T> datas)
            : base(QueryStatus.Success, null)
        {
            this.datas = datas ?? Enumerable.Empty<T>();
        }
        //public QueryMultipleResult(QueryStatus status, string message)
        //    : base(status, message)
        //{
        //    this.datas = Enumerable.Empty<T>();
        //}

        public IEnumerator<T> GetEnumerator()
        {
            return datas.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach(var item in datas)
                yield return item;
        }
    }
}
