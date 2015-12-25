using System;
using System.Runtime.Serialization;
using ThinkNet.Infrastructure;


namespace ThinkNet.Messaging
{
    /// <summary>
    /// 实现 <see cref="ICommand"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Command : Message, ICommand
    {

        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Command()
            : this(null)
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected Command(string id)
            : base(id)
        { }

        /// <summary>
        /// 获取聚合根标识的字符串形式
        /// </summary>
        protected virtual string GetAggregateRootStringId()
        {
            return string.Empty;
        }
        
        [IgnoreDataMember]
        string ICommand.AggregateRootId
        {
            get { return this.GetAggregateRootStringId(); }
        }
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
        /// 返回聚合根ID
        /// </summary>
        public override string GetRoutingKey()
        {
            return this.GetAggregateRootStringId();
        }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Concat(this.GetType().Name, "|", this.AggregateRootId);
        }
    }
}
