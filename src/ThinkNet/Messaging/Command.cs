using System;
using System.Runtime.Serialization;
using ThinkLib;
using ThinkLib.Utilities;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 实现 <see cref="ICommand"/> 的抽象类
    /// </summary>
    [DataContract]
    public abstract class Command : ICommand, IMessage//, IExtensibleDataObject
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
        {
            this.Id = id.IfEmpty(UniqueId.GenerateNewStringId);
            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 命令标识
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; private set; }
        /// <summary>
        /// 生成当前命令的时间戳
        /// </summary>
        [DataMember(Name = "timestamp")]
        public DateTime Timestamp { get; private set; }

        //private ExtensionDataObject dataobject;
        //ExtensionDataObject IExtensibleDataObject.ExtensionData
        //{
        //    get
        //    {
        //        return dataobject;
        //    }

        //    set
        //    {
        //        dataobject = value;
        //    }
        //}

        /// <summary>
        /// 获取该命令的Key
        /// </summary>
        public virtual string GetKey()
        {
            return null;
        }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}", this.GetType().FullName, this.Id);
        }
    }

    /// <summary>
    /// Represents an abstract aggregate command.
    /// </summary>
    [DataContract]
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
            aggregateRootId.NotNull("aggregateRootId");

            this.AggregateRootId = aggregateRootId;
        }

        /// <summary>
        /// 获取处理聚合命令的聚合根ID的字符串形式
        /// </summary>
        public override string GetKey()
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
