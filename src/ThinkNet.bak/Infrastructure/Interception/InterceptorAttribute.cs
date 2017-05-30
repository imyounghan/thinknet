using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ThinkNet.Infrastructure.Interception
{
    /// <summary>
    /// 表示拦截器的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public abstract class InterceptorAttribute : Attribute
    {
        private readonly static ConcurrentDictionary<Type, bool> _multiuseAttributeCache = new ConcurrentDictionary<Type, bool>();
        private int _order;

        private static bool AllowsMultiple(Type attributeType)
        {
            return _multiuseAttributeCache.GetOrAdd(
                attributeType,
                type => type.GetCustomAttributes<AttributeUsageAttribute>(true)
                            .First()
                            .AllowMultiple
            );
        }

        /// <summary>
        /// 允许多个相同的拦截器
        /// </summary>
        public bool AllowMultiple
        {
            get
            {
                return AllowsMultiple(GetType());
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order
        {
            get
            {
                return _order;
            }
            set
            {
                value.NonNegative("value");
                //if (value < -1) {
                //    throw new ArgumentOutOfRangeException("value", "Order must be greater than or equal to -1.");
                //}
                _order = value;
            }
        }

        /// <summary>
        /// 创建拦截器
        /// </summary>
        public abstract IInterceptor CreateInterceptor(IObjectContainer container);
    }
}
