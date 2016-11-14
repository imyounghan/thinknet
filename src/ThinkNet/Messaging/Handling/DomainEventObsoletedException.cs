using System;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示过时的领域事件引发的异常
    /// </summary>
    /// <remarks>
    /// 出现这个异常表示该事件已被处理过
    /// </remarks>
    public class DomainEventObsoletedException : ThinkNetException
    {
        /// <summary>
        /// 当前事件关联的聚合根类型
        /// </summary>
        public string RelatedType { get; set; }
        /// <summary>
        /// 当前事件关联的聚合根id
        /// </summary>
        public string RelatedId { get; set; }
        /// <summary>
        /// 当前事件的版本号
        /// </summary>
        public string Version { get; set; }
    }
}
