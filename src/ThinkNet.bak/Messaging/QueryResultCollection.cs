using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 查询结果集合
    /// </summary>
    [DataContract]
    public class QueryResultCollection<T> : QueryResult, IQueryResultCollection<T>
    {
        /// <summary>
        /// 空数据
        /// </summary>
        public static readonly QueryResultCollection<T> Empty = new QueryResultCollection<T>();

        [DataMember]
        private readonly List<T> list;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public QueryResultCollection()
        {
            this.list = new List<T>();
        }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public QueryResultCollection(params T[] datas)
            : this(datas as IEnumerable<T>)
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public QueryResultCollection(IEnumerable<T> datas)
        {
            this.list = new List<T>(datas);
            this.TotalRecords = list.Count;            
        }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        /// <param name="totalRecords">总记录个数</param>
        /// <param name="pageSize">每页显示记录</param>
        /// <param name="pageIndex">当前页索引</param>
        /// <param name="datas">页面数据</param>
        public QueryResultCollection(long totalRecords, int pageSize, int pageIndex, IEnumerable<T> datas)
        {
            this.TotalRecords = totalRecords;
            this.list = new List<T>(datas);
            this.PageSize = pageSize;
            this.PageIndex = pageIndex;
            this.TotalPages = (long)Math.Ceiling((double)totalRecords / (double)pageSize);
        }

        /// <summary>
        /// 获取或设置总记录数。
        /// </summary>
        [DataMember]
        public long TotalRecords { get; set; }

        /// <summary>
        /// 获取或设置页数。
        /// </summary>
        [DataMember]
        public long TotalPages { get; set; }

        /// <summary>
        /// 获取或设置页面大小。
        /// </summary>
        [DataMember]
        public int PageSize { get; set; }

        /// <summary>
        /// 获取或设置页码。
        /// </summary>
        [DataMember]
        public int PageIndex { get; set; }

        /// <summary>
        /// 获取指定索引处的元素。
        /// </summary>
        public T this[int index]
        {
            get { return list[index]; }
        }        

        /// <summary>
        /// 当前集合的个数
        /// </summary>
        public int Count
        {
            get { return list.Count; }
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举器。
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            for(int index = 0; index < list.Count; index++)
                yield return list[index];
        }
    }
}
