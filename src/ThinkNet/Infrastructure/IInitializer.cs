using System;
using System.Collections.Generic;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 应用程序初始化接口
    /// </summary>
    public interface IInitializer
    {
        /// <summary>
        /// 初始化
        /// </summary>
        void Initialize(IEnumerable<Type> types);
    }
}
