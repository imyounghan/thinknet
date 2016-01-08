using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 排序表达式接口
    /// </summary>
    public interface ISortSet<T> where T : class
    {
        /// <summary>
        /// 排序
        /// </summary>
        IEnumerable<ISortItem<T>> OrderItems { get; }

        /// <summary>
        /// 排列计算
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        IQueryable<T> Arranged(IQueryable<T> enumerable);
    }

    /// <summary>
    /// 排序元素接口
    /// </summary>
    public interface ISortItem<T>
        where T : class
    {
        /// <summary>
        /// 获取排序lambda表达式
        /// </summary>
        Expression<Func<T, dynamic>> Expression { get; }

        /// <summary>
        /// 获取排序方式
        /// </summary>
        SortOrder SortOrder { get; }
    }
}
