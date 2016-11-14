using System;
using System.Runtime.Serialization;
using ThinkNet.Common;


namespace ThinkNet.Messaging
{
    /// <summary>
    /// 实现 <see cref="ICommand"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Command : ICommand
    {

        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Command()
            : this(ObjectId.GenerateNewStringId())
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected Command(string id)
        {
            id.NotNullOrWhiteSpace("id");

            this.Id = id;
            this.Timestamp = DateTime.UtcNow;
        }

        [DataMember(Name = "id")]
        public string Id { get; private set; }
        /// <summary>
        /// 生成当前命令的时间戳
        /// </summary>
        [DataMember(Name = "timestamp")]
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// 获取聚合根标识的字符串形式
        /// </summary>
        protected virtual string GetAggregateRootStringId()
        {
            return null;
        }

        #region IMessage 成员

        string IMessage.GetKey()
        {
            return this.GetAggregateRootStringId();
        }

        #endregion
    }

    /// <summary>
    /// Represents an abstract aggregate command.
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Command<TAggregateRootId> : Command
    {
        /// <summary>
        /// Represents the aggregate root which is related with the command.
        /// </summary>
        [DataMember(Name = "aggregateRootId")]
        public TAggregateRootId AggregateRootId { get; private set; }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Command(TAggregateRootId aggregateRootId)
            : this(null, aggregateRootId)
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected Command(string commandId, TAggregateRootId aggregateRootId)
            : base(commandId)
        {
            this.AggregateRootId = aggregateRootId;
        }

        /// <summary>
        /// 获取处理聚合命令的聚合根ID的字符串形式
        /// </summary>
        protected override string GetAggregateRootStringId()
        {
            return this.AggregateRootId.ToString();
        }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}#{2}", this.GetType().FullName, this.Id, this.AggregateRootId);
        }
    }
}
