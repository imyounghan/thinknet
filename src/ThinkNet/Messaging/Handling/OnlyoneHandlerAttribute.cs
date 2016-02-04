using System;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示只有一个处理器的特性
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class OnlyoneHandlerAttribute : Attribute
    { }
}
