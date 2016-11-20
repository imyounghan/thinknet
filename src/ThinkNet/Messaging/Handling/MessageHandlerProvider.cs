using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using ThinkNet.Common.Composition;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 获取消息处理器的提供者
    /// </summary>
    public class MessageHandlerProvider
    {
        /// <summary>
        /// <see cref="MessageHandlerProvider"/> 的一个实例
        /// </summary>
        public static readonly MessageHandlerProvider Instance = new MessageHandlerProvider();

        private readonly Dictionary<CompositeKey, Type> _eventTypesMapContractType;
        private readonly ConcurrentDictionary<string, MethodInfo> _handleMethodCache;

        private MessageHandlerProvider()
        {
            this._eventTypesMapContractType = new Dictionary<CompositeKey, Type>();
            this._handleMethodCache = new ConcurrentDictionary<string, MethodInfo>();
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


        /// <summary>
        /// 通过 <param name="contractType" /> 上的泛型类型列表获取 <param name="targetType" /> 的 Handle 方法
        /// </summary>
        public MethodInfo GetHandleMethodInfo(Type targetType, Type contractType)
        {
            if(!contractType.IsInterface || !contractType.IsGenericType) {
                var errorMessage = string.Format("{0} is not a interface or generic type.", contractType.FullName);
                throw new ThinkNetException(errorMessage);
            }

            List<Type> parameTypes = new List<Type>(contractType.GenericTypeArguments);

            var genericType = contractType.GetGenericTypeDefinition();
            if(genericType == typeof(ICommandHandler<>)) {
                parameTypes.Insert(0, typeof(ICommandContext));
            }
            else if(genericType == typeof(IEventHandler<>) ||
                genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>)) {
                parameTypes.Insert(0, typeof(SourceMetadata));
            }
            else if(genericType == typeof(IMessageHandler<>)) {
            }
            else {
                var errorMessage = string.Format("{0} is unknown type.", contractType.FullName);
                throw new ThinkNetException(errorMessage);
            }

            var method = targetType.GetMethod("Handle", parameTypes.ToArray());
            if(method == null) {
                var errorMessage = string.Format("Cannot find the method 'Handle({0})' on '{1}'.",
                    string.Join(", ", parameTypes.Select(p => p.FullName).ToArray()),
                    targetType.FullName);
                throw new ThinkNetException(errorMessage);
            }

            return method;
        }

        /// <summary>
        /// 通过 <param name="contractType" /> 上的泛型类型列表从缓存中获取 <param name="targetType" /> 的 Handle 方法
        /// </summary>
        public MethodInfo GetCachedHandleMethodInfo(Type contractType, Func<Type> targetType)
        {
            var contractName = AttributedModelServices.GetContractName(contractType);

            return _handleMethodCache.GetOrAdd(contractName, delegate (string key) {
                var method = this.GetHandleMethodInfo(targetType(), contractType);
                return method;
            });
        }
    }
}
