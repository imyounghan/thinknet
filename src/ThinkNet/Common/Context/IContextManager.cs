using System;

namespace ThinkNet.Common.Context
{
    /// <summary>
    /// 实现上下文的管理接口
    /// </summary>
    public interface IContextManager
    {
        /// <summary>
        /// 上下文工厂标识
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// 获取当前的上下文
        /// </summary>
        ICurrentContext CurrentContext { get; }
    }
}
