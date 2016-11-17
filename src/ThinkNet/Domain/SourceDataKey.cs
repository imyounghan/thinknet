using System;

namespace ThinkNet.Domain
{
    public class SourceDataKey
    {
        /// <summary>
        /// 产生事件的相关标识(如命令的id)
        /// </summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// 事件源的标识id(如聚合根ID)
        /// </summary>
        public string SourceId { get; set; }
        /// <summary>
        /// 事件源的类型(如聚合根类型)
        /// </summary>
        public Type SourceType { get; set; }
    }
}
