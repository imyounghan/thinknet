
namespace ThinkNet.Communication
{
    using System.ServiceModel;

    /// <summary>
    /// 表示一个请求
    /// </summary>
    [ServiceContract(Name = "Request")]
    [DataContractFormat(Style = OperationFormatStyle.Rpc)]
    [ServiceKnownType(typeof(Response))]
    public interface IRequest
    {
        /// <summary>
        /// 发送数据不返回执行结果
        /// </summary>
        [OperationContract]
        IResponse Send(string typeName, string data);

        /// <summary>
        /// 发送数据返回执行结果
        /// </summary>
        [OperationContract]
        IResponse Execute(string typeName, string data);
    }
}
