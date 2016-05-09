using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using ThinkNet.Kernel;
using ThinkLib.Common;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示创建聚合根的接口
    /// </summary>
    [UnderlyingComponent(typeof(DefaultAggregateRootFactory))]
    public interface IAggregateRootFactory
    {
        /// <summary>
        /// 根据类型和标识创建一个聚合根
        /// </summary>
        IAggregateRoot Create(Type type, object id);

        /// <summary>
        /// 
        /// </summary>
        T Create<T>(object id) where T : class, IAggregateRoot;
    }



    internal class DefaultAggregateRootFactory : IAggregateRootFactory
    {
        private readonly ConcurrentDictionary<string, ConstructorInfo> _constructorDict;

        public DefaultAggregateRootFactory()
        {
            this._constructorDict = new ConcurrentDictionary<string, ConstructorInfo>();
        }

        public IAggregateRoot Create(Type type, object id)
        {
            if (!TypeHelper.IsEventSourced(type)) {
                var errorMessage = string.Format("the type '{0}' does not extend interface '{1}'.",
                    type.FullName, typeof(IAggregateRoot).FullName);
                throw new AggregateRootException(errorMessage);
            }

            ConstructorInfo constructor = null;
            if (!id.IsNull()) {
                constructor = _constructorDict.GetOrAdd(type.FullName, key => type.GetConstructor(new[] { id.GetType() }));
            }

            object aggregateRoot;
            if (constructor == null) {
                aggregateRoot = FormatterServices.GetUninitializedObject(type);
            }
            else {
                aggregateRoot = constructor.Invoke(new[] { id });
            }

            return aggregateRoot as IEventSourced;
        }

        public T Create<T>(object id) where T : class, IAggregateRoot
        {
            return this.Create(typeof(T), id) as T;
        }
    }
}
