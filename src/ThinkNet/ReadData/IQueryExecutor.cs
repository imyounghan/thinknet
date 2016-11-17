
namespace ThinkNet.ReadData
{
    /// <summary>
    /// 继承该接口的是查询执行器
    /// </summary>
    public interface IQueryExecutor<TParameter>
        where TParameter : class, IQueryParameter
    {
        /// <summary>
        /// 执行结果
        /// </summary>
        IQueryResult Execute(TParameter parameter);
    }
}
