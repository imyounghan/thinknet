using System.Runtime.Serialization;

namespace ThinkNet.Runtime.Kafka
{
    /// <summary>
    /// 表示这是一个可溯源的有序事件流。
    /// </summary>
    [DataContract]
    public class EventStream
    {
        /// <summary>
        /// 程序集
        /// </summary>
        [DataMember(Name = "sourceAssemblyName")]
        public string SourceAssemblyName { get; set; }
        /// <summary>
        /// 命名空间
        /// </summary>
        [DataMember(Name = "sourceNamespace")]
        public string SourceNamespace { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        [DataMember(Name = "sourceTypeName")]
        public string SourceTypeName { get; set; }

        /// <summary>
        /// 源ID
        /// </summary>
        [DataMember(Name = "sourceId")]
        public string SourceId { get; set; }

        /// <summary>
        /// 产生事件的相关标识(如命令的id)
        /// </summary>
        [DataMember(Name = "correlationId")]
        public string CorrelationId { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        [DataMember(Name = "version")]
        public int Version { get; set; }

        /// <summary>
        /// 事件源
        /// </summary>
        [DataMember(Name = "events")]
        public GeneralData[] Events { get; set; }        
        
    }
}
