using System.ServiceModel;
using System.Threading.Tasks;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示用于查询的服务
    /// </summary>
    [ServiceContract(Name = "QueryService")]
    public interface IQueryService
    {
        /// <summary>
        /// 读取数据
        /// </summary>
        [OperationContract]
        TResult Read<TResult>(IQueryParameter queryParameter)
            where TResult : QueryResult;

        /// <summary>
        /// 异步读取数据
        /// </summary>
        [OperationContract]
        Task<TResult> ReadAsync<TResult>(IQueryParameter queryParameter) 
            where TResult : QueryResult;

    }
}
