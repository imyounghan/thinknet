using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using ThinkNet.Annotation;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;


namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示创建聚合根的接口
    /// </summary>
    [RequiredComponent(typeof(DefaultEventSourcedFactory))]
    public interface IEventSourcedFactory
    {
        /// <summary>
        /// 根据类型和标识创建一个聚合根
        /// </summary>
        IEventSourced Create(Type type, object id);
    }



    internal class DefaultEventSourcedFactory : IEventSourcedFactory
    {
        private readonly ConcurrentDictionary<string, ConstructorInfo> _constructorDict;

        public DefaultEventSourcedFactory()
        {
            this._constructorDict = new ConcurrentDictionary<string, ConstructorInfo>();
        }

        public IEventSourced Create(Type type, object id)
        {
            if (!TypeHelper.IsEventSourced(type)) {
                var errorMessage = string.Format("the type '{0}' does not extend interface '{1}'.",
                    type.FullName, typeof(IEventSourced).FullName);
                throw new EventSourcedException(errorMessage);
            }


            var constructor = _constructorDict.GetOrAdd(type.FullName, key => type.GetConstructor(new[] { id.GetType() }));

            object aggregateRoot;
            if (constructor == null) {
                aggregateRoot = FormatterServices.GetUninitializedObject(type);
            }
            else {
                aggregateRoot = constructor.Invoke(new[] { id });
            }

            return aggregateRoot as IEventSourced;
        }
    }
}
