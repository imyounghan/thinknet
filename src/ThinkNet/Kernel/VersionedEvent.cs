using System;
using System.Runtime.Serialization;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// Represents an abstract sourced event.
    /// </summary>
    [DataContract]
    public abstract class VersionedEvent<TSourceId> : Event<TSourceId>, IVersionedEvent
    {
        /// <summary>
        /// 当前事件版本号
        /// </summary>
        [DataMember]
        public int Version { get; internal set; }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Concat(this.GetType().FullName, "@", this.SourceId, "@", this.Version);
        }
    }
}
