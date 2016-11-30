
namespace ThinkNet.Domain.EventSourcing
{
    public interface IPublishedVersionStore
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
