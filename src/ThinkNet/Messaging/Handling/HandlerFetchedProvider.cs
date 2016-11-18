using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common.Composition;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 获取消息处理器的提供者
    /// </summary>
    public class HandlerFetchedProvider
    {
        /// <summary>
        /// <see cref="HandlerFetchedProvider"/> 的一个实例
        /// </summary>
        public static readonly HandlerFetchedProvider Instance = new HandlerFetchedProvider();

        private readonly Dictionary<CompositeKey, Type> _eventTypesMapContractType;

        private HandlerFetchedProvider()
        {
            this._eventTypesMapContractType = new Dictionary<CompositeKey, Type>();
        }

        /// <summary>
        /// 获取命令处理器
        /// </summary>
        public IEnumerable<object> GetCommandHandlers(Type commandType, out Type contractType)
        {
            contractType = typeof(ICommandHandler<>).MakeGenericType(commandType);

            return ObjectContainer.Instance.ResolveAll(contractType);
        }

        /// <summary>
        /// 获取消息处理器
        /// </summary>
        public IEnumerable<object> GetMessageHandlers(Type messageType, out Type contractType)
        {
            contractType = typeof(IMessageHandler<>).MakeGenericType(messageType);

            return ObjectContainer.Instance.ResolveAll(contractType);
        }

        /// <summary>
        /// 获取事件对应的处理器接口类型
        /// </summary>
        public Type GetEventHandlerType(Type[] eventTypes)
        {
            switch(eventTypes.Length) {
                case 0:
                    throw new ArgumentNullException("eventTypes", "An empty array.");
                case 1:
                    return typeof(IEventHandler<>).MakeGenericType(eventTypes[0]);
                default:
                    return _eventTypesMapContractType[new CompositeKey(eventTypes)];
            }
        }

        /// <summary>
        /// 通过事件处理器接口类型获取相应的处理器实例
        /// </summary>
        public IEnumerable<object> GetEventHandlers(Type contractType)
        {
            return ObjectContainer.Instance.ResolveAll(contractType);
        }

        /// <summary>
        /// 获取事件处理器并输出处理器接口类型
        /// </summary>
        public IEnumerable<object> GetEventHandlers(Type[] eventTypes, out Type contractType)
        {
            contractType = GetEventHandlerType(eventTypes);

            return GetEventHandlers(contractType);
        }

        private static bool IsEventHandlerInterface(Type type)
        {
            if(!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>);
        }

        /// <summary>
        /// 初始化程序
        /// </summary>
        public void Initialize(IEnumerable<Type> types)
        {
            foreach(var type in types.Where(p => p.IsClass && !p.IsAbstract)) {
                foreach(var interfaceType in type.GetInterfaces().Where(IsEventHandlerInterface)) {
                    var key = new CompositeKey(interfaceType.GenericTypeArguments);
                    _eventTypesMapContractType.TryAdd(key, interfaceType);
                }
            }
        }

        struct CompositeKey : IEnumerable<Type>
        {
            private readonly IEnumerable<Type> types;

            public CompositeKey(IEnumerable<Type> types)
            {
                if(types.Distinct().Count() != types.Count()) {
                    throw new ArgumentException("There are have duplicate types.", "types");
                }

                this.types = types;
            }

            public IEnumerator<Type> GetEnumerator()
            {
                return types.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach(var type in types) {
                    yield return type;
                }
            }

            public override bool Equals(object obj)
            {
                if(obj == null || obj.GetType() != this.GetType())
                    return false;
                var other = (CompositeKey)obj;
                
                return this.Except(other).IsEmpty();
            }

            public override int GetHashCode()
            {
                return types.OrderBy(type => type.FullName).Select(type => type.GetHashCode()).Aggregate((x, y) => x ^ y);
            }
        }
    }
}
