using System;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示待处理的领域事件引发的异常
    /// </summary>
    /// <remarks>
    /// 出现这个异常表示该事件被过早的处理
    /// </remarks>
    public class DomainEventAsPendingException : ThinkNetException
    {
        /// <summary>
        /// 当前事件关联的聚合根类型
        /// </summary>
        public string RelatedType { get; set; }
        /// <summary>
        /// 当前事件关联的聚合根Id
        /// </summary>
        public string RelatedId { get; set; }
        /// <summary>
        /// 当前事件的版本号
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// 已发布的版本号
        /// </summary>
        public int PublishedVersion { get; set; }
    }
}
