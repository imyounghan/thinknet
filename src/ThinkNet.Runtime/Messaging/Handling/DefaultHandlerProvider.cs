using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Common;
using ThinkNet.Infrastructure;


namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// <see cref="IHandlerProvider"/> 的默认实现
    /// </summary>
    internal class DefaultHandlerProvider : IHandlerProvider, IInitializer
    {
        //private readonly Type _handlerGenericType;
        //private readonly ConcurrentDictionary<Type, ICollection<IProxyHandler>> _handerDict;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public DefaultHandlerProvider()
        {
            //this._handlerGenericType = typeof(IHandler<>);
            //this._handerDict = new ConcurrentDictionary<Type, ICollection<IProxyHandler>>();
        }

        void IInitializer.Initialize(IEnumerable<Type> types)
        {
            //var types = assemblies.SelectMany(assembly => assembly.GetTypes());
            //types.Where(IsRegisterType).ForEach(type => RegisterType(type, container));
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        private void RegisterType(Type type)
        {
            //LifetimeType lifetime = LifetimeType.Singleton;
            //if (type.IsDefined(typeof(LifeCycleAttribute), false)) {
            //    lifetime = (LifetimeType)type.GetAttribute<LifeCycleAttribute>(false).Lifecycle;
            //}

            //var interfaceTypes = type.GetInterfaces().Where(IsGenericType);
            //if (lifetime == LifetimeStyle.Singleton) {
            //    var handlerConstructor = type.GetConstructors().FirstOrDefault();
            //    if (handlerConstructor != null) {
            //        var parameters= handlerConstructor.GetParameters().Select(p => ObjectContainer.Current.Resolve(p.ParameterType)).ToArray();
            //        var handler = handlerConstructor.Invoke(parameters);

            //        foreach (var interfaceType in interfaceTypes) {
            //            this.Register(interfaceType, handler);
            //        }

            //        return;
            //    }
            //}

            //foreach (var interfaceType in interfaceTypes) {
            //    container.RegisterType(interfaceType, type, type.FullName, lifetime);
            //}
        }

        //private void Register(Type eventHandlerType, object eventHandler)
        //{
        //    var eventType = eventHandlerType.GetGenericArguments().First();
        //    var handlerWrapperType = typeof(HandlerWrapper<>).MakeGenericType(eventType);
        //    var proxy = Activator.CreateInstance(handlerWrapperType, new[] { eventHandler }) as IProxyHandler;

        //    _handerDict.AddOrUpdate(eventType, key => {
        //        var handers = new List<IProxyHandler>();
        //        if (proxy != null) {
        //            handers.Add(proxy);
        //        }
        //        return handers;
        //    }, (key, value) => {
        //        if (proxy != null) {
        //            value.Add(proxy);
        //        }
        //        return value;
        //    });
        //}

        /// <summary>
        /// 判断是否为事件处理接口
        /// </summary>
        private bool IsGenericType(Type type)
        {
            return type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == _handlerGenericType;
        }

        /// <summary>
        /// 判断是否为事件处理程序类型
        /// </summary>
        private bool IsRegisterType(Type type)
        {
            return type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(IsGenericType);
        }

        private Type MakeProxyHandlerType(Type type)
        {
            return _handlerGenericType.MakeGenericType(type);
        }

        /// <summary>
        /// 获取该事件类型的处理器
        /// </summary>
        public IEnumerable<IProxyHandler> GetHandlers(Type type)
        {
            List<object> handlerlist = new List<object>();

            var handlerType = typeof(IMessageHandler<>).MakeGenericType(type);
            var handlers = ServiceLocator.Current.GetAllInstances(handlerType);
            handlerlist.AddRange(handlers);

            handlerType = typeof(ICommandHandler<>).MakeGenericType(type);
            handlers = ServiceLocator.Current.GetAllInstances(handlerType);
            handlerlist.AddRange(handlers);

            handlerType = typeof(IEventHandler<>).MakeGenericType(type);
            handlers = ServiceLocator.Current.GetAllInstances(handlerType);
            handlerlist.AddRange(handlers);


            return handlerlist.Select(handler => {
                var handlerWrapperType = typeof(HandlerWrapper<>).MakeGenericType(type);
                return Activator.CreateInstance(handlerWrapperType, new[] { handler });
            }).OfType<IProxyHandler>().AsEnumerable();
        }
    }
}
