
namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示一个服务的网关接口
    /// </summary>
    public interface IServiceGateway
    {
        /// <summary>
        /// 获取服务
        /// </summary>
        TService GetService<TService>();
    }
}
