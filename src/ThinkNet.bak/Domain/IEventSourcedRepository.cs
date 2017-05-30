using System;

namespace ThinkNet.Domain
{
    /// <summary>
    /// 表示继承该接口的是一个仓储。
    /// </summary>
    public interface IEventSourcedRepository
    {
        /// <summary>
        /// 查找聚合。如果不存在返回null，存在返回实例
        /// </summary>
        IEventSourced Find(Type eventSourcedType, object eventSourcedId);

        /// <summary>
        /// 保存聚合根。
        /// </summary>
        void Save(IEventSourced eventSourced, string correlationId);

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        void Delete(Type eventSourcedType, object eventSourcedId);
    }
}
