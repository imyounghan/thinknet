using System.Collections.Generic;

namespace ThinkNet.Messaging

{
    /// <summary>
    /// 表示这是一个持久化消息的接口
    /// </summary>
    public interface IMessageStore
    {
        /// <summary>
        /// 是否启用持久化存储。
        /// </summary>
        bool PersistEnabled { get; }

        /// <summary>
        /// Persist a new message to the store.
        /// </summary>
        void Add(IEnumerable<Message> messages);
        /// <summary>
        /// Remove a existing message from the store.
        /// </summary>
        void Remove(Message message);
        /// <summary>
        /// Get all the existing messages of the store.
        /// </summary>
        IEnumerable<Message> GetAll();
    }
}
