

namespace ThinkNet.Messaging
{
    using System.Collections;
    using System.Collections.Generic;

    public class PageResult<TData>
    {
        public PageResult()
        { }

        public PageResult(IEnumerable<TData> data)
        {
            this.Data = data;
        }

        public PageResult(IEnumerable<TData> data, long total)
        {
            this.Data = data;
        }

        public IEnumerable<TData> Data { get; set; }

        //public int Count { get; set; }

        /// <summary>
        /// 获取或设置总记录数。
        /// </summary>
        public long TotalRecords { get; set; }
    }
}
