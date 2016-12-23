using System.Threading.Tasks;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示用于查询的服务
    /// </summary>
    public interface IQueryService
    {
        /// <summary>
        /// 读取数据
        /// </summary>
        IQueryResult Execute(IQuery query);
        
        /// <summary>
        /// 异步读取数据
        /// </summary>
        Task<IQueryResult> ExecuteAsync(IQuery query);

    }
}
