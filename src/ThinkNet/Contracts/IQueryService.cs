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
        IQueryResult Execute(IQueryParameter queryParameter);

        ///// <summary>
        ///// 读取数据
        ///// </summary>
        //[OperationContract]
        //IQueryResult Read(IPageQueryParameter queryParameter);

        /// <summary>
        /// 异步读取数据
        /// </summary>
        [OperationContract]
        Task<IQueryResult> ExecuteAsync(IQueryParameter queryParameter);

        ///// <summary>
        ///// 异步读取数据
        ///// </summary>
        //[OperationContract]
        //Task<IQueryResult> ReadAsync(IPageQueryParameter queryParameter);

    }
}
