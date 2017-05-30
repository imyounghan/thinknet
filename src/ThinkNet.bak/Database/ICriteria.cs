using System;
using System.Linq;
using System.Linq.Expressions;

namespace ThinkNet.Database
{
    /// <summary>
    /// 查询接口
    /// </summary>
    public interface ICriteria<T> where T : class
    {
        /// <summary>
        /// 获取lambda表达式
        /// </summary>
        Expression<Func<T, bool>> Expression { get; }

        /// <summary>
        /// 数据过滤
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        IQueryable<T> Filtered(IQueryable<T> enumerable);
    }
}
