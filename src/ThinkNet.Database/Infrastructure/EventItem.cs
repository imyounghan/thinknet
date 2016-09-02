
namespace ThinkNet.Infrastructure
{
    public class EventItem
    {
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
        /// 事件ID
        /// </summary>
        public string EventId { get; set; }
        /// <summary>
        /// 事件流
        /// </summary>
        public byte[] Payload { get; set; }
    }
}
