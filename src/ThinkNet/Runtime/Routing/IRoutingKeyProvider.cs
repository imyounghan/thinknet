
namespace ThinkNet.Runtime.Routing
{
    /// <summary>
    /// 表示一个获取路由值的程序
    /// </summary>
    public interface IRoutingKeyProvider
    {
        /// <summary>
        /// 获取该数据的路由值
        /// </summary>
        string GetRoutingKey(object payload);
    }    
}
