using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ThinkNet.Messaging
{
    [DataContract]
    [Serializable]
    public class EventStream
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventStream()
        { }

        public EventStream(string sourceId, Type sourceType)
            : this(sourceId, sourceType.Namespace, sourceType.Name, sourceType.GetAssemblyName())
        { }

        public EventStream(string sourceId, string sourceNamespace, string sourceTypeName, string sourceAssemblyName)
        {
            this.SourceId = sourceId;
            this.SourceNamespace = sourceNamespace;
            this.SourceTypeName = sourceTypeName;
            this.SourceAssemblyName = sourceAssemblyName;
        }

        /// <summary>
        /// 事件标识
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 程序集
        /// </summary>
        [DataMember]
        public string SourceAssemblyName { get; set; }
        /// <summary>
        /// 命名空间
        /// </summary>
        [DataMember]
        public string SourceNamespace { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        [DataMember]
        public string SourceTypeName { get; set; }
        /// <summary>
        /// 标识。
        /// </summary>
        [DataMember]
        public string SourceId { get; set; }
        /// <summary>
        /// 产生事件的命令标识
        /// </summary>
        [DataMember]
        public string CommandId { get; set; }
        /// <summary>
        /// 起始版本号
        /// </summary>
        [DataMember]
        public int Version { get; set; }
        ///// <summary>
        /// 事件源
        /// </summary>
        [DataMember]
        public IEnumerable<GeneralData> Events { get; set; }

        [DataMember]
        public DateTime CreatedTime { get; set; }

        public Type GetSourceType()
        {
            string typeFullName = string.Concat(this.SourceNamespace, ".", this.SourceTypeName, ", ", this.SourceAssemblyName);
            return Type.GetType(typeFullName);
        }
    }
}
