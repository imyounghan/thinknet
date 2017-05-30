using System;

namespace ThinkNet.Infrastructure.Storage
{
    /// <summary>
    /// 事件包里的项
    /// </summary>
    public class EventDataItem
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public EventDataItem()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventDataItem(Type eventType)
        {
            this.AssemblyName = eventType.GetAssemblyName();
            this.Namespace = eventType.Namespace;
            this.TypeName = eventType.Name;
        }

        /// <summary>
        /// 事件ID
        /// </summary>
        public long EventId { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 程序集
        /// </summary>
        public string AssemblyName { get; set; }
        /// <summary>
        /// 程序集
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        public string TypeName { get; set; }        
        /// <summary>
        /// 事件流
        /// </summary>
        public byte[] Payload { get; set; }
    }
}
