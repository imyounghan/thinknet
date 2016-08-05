
namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示一个存储器用来存储聚合事件的发布版本号。
    /// </summary>
    public interface IEventPublishedVersionStore
    {
        /// <summary>
        /// 更新版本号
        /// </summary>
        void AddOrUpdatePublishedVersion(DataKey sourceKey, int version);
        /// <summary>
        /// 获取已发布的版本号
        /// </summary>
        int GetPublishedVersion(DataKey sourceKey);
    }
}
