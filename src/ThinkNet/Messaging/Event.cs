using System;
using System.Runtime.Serialization;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 实现 <see cref="IEvent"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Event : Message, IEvent
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Event()
            : base()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Event(string id)
            : base(id)
        { }
        
        /// <summary>
        /// 获取源标识的字符串形式
        /// </summary>
        public virtual string GetSourceStringId()
        {
            return string.Empty;
        }
    }    
}
