using System;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示只处理一次的特性
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class JustHandleOnceAttribute : Attribute
    { }
}
