using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Kernel
{
    public static class AggregateRootInnerHandlerProvider
    {
        private readonly static ConcurrentDictionary<Type, IDictionary<Type, MethodInfo>> _innerHandlers;

        static AggregateRootInnerHandlerProvider()
        {
            _innerHandlers = new ConcurrentDictionary<Type, IDictionary<Type, MethodInfo>>();
        }

        private static IDictionary<Type, MethodInfo> FindInnerHandlers(Type type)
        {
            var eventHandlerDic = new Dictionary<Type, MethodInfo>();

            var entries = from method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                          let returnType = method.ReturnType
                          let parameters = method.GetParameters()
                          let parameter = parameters.FirstOrDefault()
                          where method.Name.ToLower() == "handle"
                            && returnType == typeof(void)
                            && parameters.Length == 1
                            && typeof(IEvent).IsAssignableFrom(parameter.ParameterType)
                          select new { Method = method, EventType = parameter.ParameterType };
            foreach (var entry in entries) {
                if (eventHandlerDic.ContainsKey(entry.EventType)) {
                    var errorMessage = string.Format("found duplicated event handler on aggregateroot. aggregateroot type:{0}, event type:{1}.",
                        type.FullName, entry.EventType.FullName);
                    throw new AggregateRootException(errorMessage);
                }
                eventHandlerDic.Add(entry.EventType, entry.Method);
            }

            return eventHandlerDic;
        }

        private static void Scan(Type type)
        {
            if (!type.IsSerializable) {
                string message = string.Format("{0} should be marked as serializable.", type.FullName);
                throw new ApplicationException(message);
            }

            var handlers = FindInnerHandlers(type);
            _innerHandlers.TryAdd(type, handlers);
        }

        public static void Initialize(IEnumerable<Type> types)
        {
            types.Where(TypeHelper.IsAggregateRoot).ForEach(Scan);
        }

        /// <summary>
        /// 获取聚合内部事件处理器
        /// </summary>
        public static Action<IAggregateRoot, IEvent> GetEventHandler(Type aggregateRootType, Type eventType)
        {
            var eventHandlerDic = _innerHandlers.GetOrAdd(aggregateRootType, FindInnerHandlers);

            if (eventHandlerDic == null || eventHandlerDic.Count == 0)
                return null;


            MethodInfo eventHandler;
            return eventHandlerDic.TryGetValue(eventType, out eventHandler) ?
                new Action<IAggregateRoot, IEvent>((aggregateRoot, @event) => eventHandler.Invoke(aggregateRoot, new[] { @event })) : null;
        }

    }
}
