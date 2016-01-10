using System;
using System.Linq;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// 表示具有版本号的事件流
    /// </summary>
    [Serializable]
    public class VersionedEventStream : EventStream
    {
        /// <summary>
        /// 起始版本号
        /// </summary>
        public int StartVersion { get; set; }
        /// <summary>
        /// 结束版本号
        /// </summary>
        public int EndVersion { get; set; }
    }
}
