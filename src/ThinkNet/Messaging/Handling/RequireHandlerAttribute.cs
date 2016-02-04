using System;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示必需有相应处理器的特性
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RequireHandlerAttribute : Attribute
    { }
}
