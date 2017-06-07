
namespace ThinkNet.Contracts
{
    public interface IQueryService
    {
        /// <summary>
        /// 读取数据
        /// </summary>
        IQueryResult Execute(IQuery query);
    }
}
