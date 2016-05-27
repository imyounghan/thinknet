using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    /// <summary>
    /// 事件流
    /// </summary>
    [DataContract]
    [Serializable]
    internal class EventStream : Event
    {
        [DataContract]
        [Serializable]
        public class Stream
        {
            public Stream()
            { }

            public Stream(Type sourceType)
                : this(sourceType.Namespace,
                sourceType.Name,
                Path.GetFileNameWithoutExtension(sourceType.Assembly.ManifestModule.FullyQualifiedName))
            { }

            public Stream(string sourceTypeName)
                : this(string.Empty, sourceTypeName, string.Empty)
            { }
            public Stream(string sourceNamespace, string sourceTypeName)
                : this(sourceNamespace, sourceTypeName, string.Empty)
            { }

            public Stream(string sourceNamespace, string sourceTypeName, string sourceAssemblyName)
            {
                this.Namespace = sourceNamespace;
                this.TypeName = sourceTypeName;
                this.AssemblyName = sourceAssemblyName;
            }

            /// <summary>
            /// 程序集
            /// </summary>
            [DataMember]
            public string AssemblyName { get; set; }
            /// <summary>
            /// 命名空间
            /// </summary>
            [DataMember]
            public string Namespace { get; set; }
            /// <summary>
            /// 类型名称(不包含全名空间)
            /// </summary>
            [DataMember]
            public string TypeName { get; set; }

            /// <summary>
            /// 流数据
            /// </summary>
            [DataMember]
            public string Payload { get; set; }

            public override string ToString()
            {
                return string.Concat(this.Namespace, ".", this.TypeName, "@", this.Payload);
            }

            public Type GetSourceType()
            {
                string typeFullName = string.Concat(this.Namespace, ".", this.TypeName, ", ", this.AssemblyName);

                return Type.GetType(typeFullName);
            }
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventStream()
        { }

        public EventStream(object sourceId, Type sourceType)
            : this(sourceId.ToString(),
            sourceType.Namespace,
            sourceType.Name,
            Path.GetFileNameWithoutExtension(sourceType.Assembly.ManifestModule.FullyQualifiedName))
        { }

        public EventStream(string sourceId, string sourceNamespace, string sourceTypeName)
            : this(sourceId, sourceNamespace, sourceTypeName, string.Empty)
        { }

        public EventStream(string sourceId, string sourceNamespace, string sourceTypeName, string sourceAssemblyName)
        {
            this.SourceId = sourceId;
            this.SourceNamespace = sourceNamespace;
            this.SourceTypeName = sourceTypeName;
            this.SourceAssemblyName = sourceAssemblyName;
        }

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
        public int StartVersion { get; set; }
        /// <summary>
        /// 结束版本号
        /// </summary>
        [DataMember]
        public int EndVersion { get; set; }
        /// <summary>
        /// 事件源
        /// </summary>
        [DataMember]
        public IEnumerable<Stream> Events { get; set; }

        public override string GetSourceStringId()
        {
            return this.SourceId;
        }


        /// <summary>
        /// 输出领域事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            return string.Concat("[", string.Join("^", this.Events), "]", "#", this.CommandId);
        }
    }
}
