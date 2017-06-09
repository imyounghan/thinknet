
namespace ThinkNet.Messaging.Handling
{
    using System.Collections.Generic;

    /// <summary>
    /// 用于分页查询的读取程序 
    /// </summary>
    public interface IPageQueryHandler<TQuery, TResult> : IHandler
        where TQuery : IPageQuery
    {
        /// <summary>
        /// 获取结果
        /// </summary>
        IEnumerable<TResult> Fetch(TQuery parameter, out long total);
    }
}
