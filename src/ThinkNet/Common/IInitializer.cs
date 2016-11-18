using System.Collections.Generic;
using System.Reflection;
using ThinkNet.Common.Composition;

namespace ThinkNet.Common
{
    /// <summary>
    /// 应用程序初始化接口
    /// </summary>
    public interface IInitializer
    {
        /// <summary>
        /// 初始化
        /// </summary>
        void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies);
    }
}
