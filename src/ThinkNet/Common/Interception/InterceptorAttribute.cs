using System;
using System.Collections.Concurrent;
using System.Linq;
using ThinkNet.Common.Composition;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Common.Interception
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public abstract class InterceptorAttribute : Attribute
    {
        private readonly static ConcurrentDictionary<Type, bool> _multiuseAttributeCache = new ConcurrentDictionary<Type, bool>();
        private int _order = -1;

        private static bool AllowsMultiple(Type attributeType)
        {
            return _multiuseAttributeCache.GetOrAdd(
                attributeType,
                type => type.GetCustomAttributes(typeof(AttributeUsageAttribute), true)
                            .Cast<AttributeUsageAttribute>()
                            .First()
                            .AllowMultiple
            );
        }

        public bool AllowMultiple
        {
            get
            {
                return AllowsMultiple(GetType());
            }
        }

        public int Order
        {
            get
            {
                return _order;
            }
            set
            {
                if (value < -1) {
                    throw new ArgumentOutOfRangeException("value", "Order must be greater than or equal to -1.");
                }
                _order = value;
            }
        }

        public abstract IInterceptor CreateInterceptor(IObjectContainer container);
    }
}
