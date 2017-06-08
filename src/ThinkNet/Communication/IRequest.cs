
namespace ThinkNet.Communication
{
    /// <summary>
    /// 表示一个请求
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// 发送数据不返回执行结果
        /// </summary>
        IResponse Send(string type, string data);
        /// <summary>
        /// 发送数据返回执行结果
        /// </summary>
        IResponse Execute(string type, string data);
    }
}
