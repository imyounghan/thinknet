
namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示继承该接口的是具有提供关键字的功能
    /// </summary>
    public interface IKeyProvider
    {
        /// <summary>
        /// 获取关键字
        /// </summary>
        string GetKey();
    }
}
