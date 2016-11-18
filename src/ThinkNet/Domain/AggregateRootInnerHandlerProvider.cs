using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Messaging;

namespace ThinkNet.Domain
{
    /// <summary>
    /// 表示这是一个获取聚合根内部处理器的提供者
    /// </summary>
    public sealed class AggregateRootInnerHandlerProvider
    {
        /// <summary>
        /// <see cref="AggregateRootInnerHandlerProvider"/> 的一个实例
        /// </summary>
        public static readonly AggregateRootInnerHandlerProvider Instance = new AggregateRootInnerHandlerProvider();

        private readonly Dictionary<Type, IDictionary<Type, MethodInfo>> _innerHandlers;

        private AggregateRootInnerHandlerProvider()
        {
            this._innerHandlers = new Dictionary<Type, IDictionary<Type, MethodInfo>>();
        }

        private IDictionary<Type, MethodInfo> FindInnerHandlers(Type type)
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
                    throw new ThinkNetException(errorMessage);
                }
                eventHandlerDic.Add(entry.EventType, entry.Method);
            }

            return eventHandlerDic;
        }

        private static bool IsAggregateRoot(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IAggregateRoot).IsAssignableFrom(type);
        }

        /// <summary>
        /// 初始化聚合根内部处理器并提供缓存能力。
        /// </summary>
        public void Initialize(IEnumerable<Type> types)
        {
            foreach (var type in types.Where(IsAggregateRoot)) {
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
        private Action<IAggregateRoot, IEvent> GetHandler(Type aggregateRootType, Type eventType)
        {
            IDictionary<Type, MethodInfo> eventHandlerDic;
            MethodInfo targetMethod;
            if (!_innerHandlers.TryGetValue(aggregateRootType, out eventHandlerDic) ||
                eventHandlerDic == null || !eventHandlerDic.TryGetValue(eventType, out targetMethod)) {
                return delegate { };
            }

            return delegate(IAggregateRoot aggregateRoot, IEvent @event) {
                targetMethod.Invoke(aggregateRoot, new[] { @event });
            };

            //MethodInfo eventHandler;
            //return eventHandlerDic.TryGetValue(eventType, out eventHandler) ?
            //    new Action<IAggregateRoot, IEvent>((aggregateRoot, @event) => eventHandler.Invoke(aggregateRoot, new[] { @event })) : null;
        }

        /// <summary>
        /// 判断聚合内部是否存在该事件处理器
        /// </summary>
        public bool HasHandler(Type aggregateRootType, Type eventType)
        {
            IDictionary<Type, MethodInfo> eventHandlerDic;

            return _innerHandlers.TryGetValue(aggregateRootType, out eventHandlerDic) &&
                eventHandlerDic != null && eventHandlerDic.ContainsKey(eventType);
        }

        /// <summary>
        /// 处理聚合内部事件
        /// </summary>
        public void Handle(IAggregateRoot aggregateRoot, IEvent @event)
        {
            var eventType = @event.GetType();
            var aggregateRootType = aggregateRoot.GetType();

            GetHandler(aggregateRootType, eventType).Invoke(aggregateRoot, @event);

            //if (innerHandler == null) {
            //    return;
            //    var errorMessage = string.Format("Event handler not found on {0} for {1}.",
            //        aggregateRootType.FullName, eventType.FullName);
            //    throw new ThinkNetException(errorMessage);
            //}

            //innerHandler.Invoke(aggregateRoot, @event);
        }
    }
}
