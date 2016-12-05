using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Database
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
}
