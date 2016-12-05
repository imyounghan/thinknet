using System;
using System.Data.SqlClient;
using System.Linq.Expressions;

namespace ThinkNet.Database
{
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
