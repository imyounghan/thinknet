using ThinkLib.Common;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示一个存储器用来存储聚合事件的发布版本号。
    /// </summary>
    [RequiredComponent(typeof(EventPublishedVersionInMemory))]
    public interface IEventPublishedVersionStore
    {
        /// <summary>
        /// 更新版本号
        /// </summary>
        void AddOrUpdatePublishedVersion(SourceKey sourceKey, int startVersion, int endVersion);
        /// <summary>
        /// 获取已发布的版本号
        /// </summary>
        int GetPublishedVersion(SourceKey sourceKey);
    }
}
