using System;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示必需有相应处理器的特性
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RequireHandlerAttribute : Attribute
    {

        public RequireHandlerAttribute()
        { }

        public RequireHandlerAttribute(bool allowMultiple)
        {
            this.AllowMultiple = allowMultiple;
        }

        /// <summary>
        /// 是否允许多个Handler
        /// </summary>
        public bool AllowMultiple { get; private set; }
    }
}
