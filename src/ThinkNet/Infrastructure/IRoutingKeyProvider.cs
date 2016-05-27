
namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示一个获取路由值的程序
    /// </summary>
    public interface IRoutingKeyProvider
    {
        string GetRoutingKey(object payload);
    }    
}
