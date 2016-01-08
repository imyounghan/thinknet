using System;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示只处理一次的特性
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class HandleOnlyOnceAttribute : Attribute
    { }
}
