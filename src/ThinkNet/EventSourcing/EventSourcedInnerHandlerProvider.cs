using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示这是一个获取聚合根内部处理器的提供者
    /// </summary>
    public static class EventSourcedInnerHandlerProvider
    {
        private readonly static ConcurrentDictionary<Type, IDictionary<Type, MethodInfo>> _innerHandlers;

        static EventSourcedInnerHandlerProvider()
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
                    var errorMessage = string.Format("found duplicated handler from '{0}' on '{1}'.", 
                        entry.EventType.FullName, type.FullName);
                    throw new EventSourcedException(errorMessage);
                }
                eventHandlerDic.Add(entry.EventType, entry.Method);
            }

            return eventHandlerDic;
        }

        /// <summary>
        /// 初始化聚合根内部处理器并提供缓存能力。
        /// </summary>
        public static void Initialize(IEnumerable<Type> types)
        {
            foreach (var type in types.Where(TypeHelper.IsEventSourced)) {
                if (!type.IsSerializable) {
                    string message = string.Format("{0} should be marked as serializable.", type.FullName);
                    throw new ApplicationException(message);
                }

                var handlers = FindInnerHandlers(type);
                _innerHandlers.TryAdd(type, handlers);
            }
        }

        /// <summary>
        /// 获取聚合内部事件处理器
        /// </summary>
        public static Action<IEventSourced, IEvent> GetEventHandler(Type aggregateRootType, Type eventType)
        {
            var eventHandlerDic = _innerHandlers.GetOrAdd(aggregateRootType, FindInnerHandlers);

            if (eventHandlerDic == null || eventHandlerDic.Count == 0)
                return null;


            MethodInfo eventHandler;
            return eventHandlerDic.TryGetValue(eventType, out eventHandler) ?
                new Action<IEventSourced, IEvent>((aggregateRoot, @event) => eventHandler.Invoke(aggregateRoot, new[] { @event })) : null;
        }

    }
}
