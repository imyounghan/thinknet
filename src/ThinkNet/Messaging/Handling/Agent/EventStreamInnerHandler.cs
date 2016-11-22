using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using ThinkLib.Annotation;
using ThinkLib.Composition;
using ThinkLib.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// <see cref="EventStream"/> 的内部处理程序
    /// </summary>
    public class EventStreamInnerHandler : HandlerAgent
    {
        private readonly ConcurrentDictionary<Type, IHandlerAgent> _cachedHandlers;
        private readonly Lazy<MethodInfo> _method;
        

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventStreamInnerHandler(InterceptorPipeline pipeline)
            : base(pipeline)
        {
            this._method = new Lazy<MethodInfo>(GetMethodInfo);
            this._cachedHandlers = new ConcurrentDictionary<Type, IHandlerAgent>();
        }

        private static MethodInfo GetMethodInfo()
        {
            return typeof(EventStreamInnerHandler).GetMethod("Handle", 
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null,
                new[] { typeof(EventStream) },
                null);
        }

        /// <summary>
        /// 处理 <see cref="EventStream"/> 的反射方法
        /// </summary>
        public override MethodInfo ReflectedMethod { get { return _method.Value; } }

        /// <summary>
        /// 处理 <see cref="EventStream"/> 的实例
        /// </summary>
        public override object HandlerInstance { get { return this; } }


        protected override void TryMultipleHandle(object[] args)
        {
            var eventStream = args[0] as EventStream;

            if (eventStream == null) {
                //TODO..
                return;
            }

            this.Handle(eventStream);
        }

        private void Handle(EventStream eventStream)
        {
            var eventTypes = eventStream.Events.Select(p => p.GetType()).ToArray();
            var eventHandler = this.GetEventHandler(eventTypes);
            var parameters = this.GetParameters(eventStream);

            eventHandler.Handle(parameters);
        }

        private IHandlerAgent BuildEventHandler(object handler, Type contractType)
        {
            var method = MessageHandlerProvider.Instance.GetCachedHandleMethodInfo(contractType, () => handler.GetType());
            return new EventHandlerAgent(handler, method);
        }

        /// <summary>
        /// 获取处理事件的代理
        /// </summary>
        protected IHandlerAgent GetEventHandler(Type[] types)
        {
            var contractType = MessageHandlerProvider.Instance.GetEventHandlerType(types);

            IHandlerAgent cachedHandler;
            if(_cachedHandlers.TryGetValue(contractType, out cachedHandler))
                return cachedHandler;
            
            var handlers = MessageHandlerProvider.Instance.GetEventHandlers(contractType)
                .Select(handler => BuildEventHandler(handler, contractType))
                .ToArray();

            switch(handlers.Length) {
                case 0:
                    throw new MessageHandlerNotFoundException(types);
                case 1:
                    var handler = handlers[0];
                    var lifecycle = LifeCycleAttribute.GetLifecycle(handler.ReflectedMethod.DeclaringType);
                    if(lifecycle == Lifecycle.Singleton)
                        _cachedHandlers.TryAdd(contractType, handler);
                    return handler;
                default:
                    throw new MessageHandlerTooManyException(types);
            }
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        protected object[] GetParameters(EventStream eventStream)
        {
            var array = new ArrayList();
            array.Add(new SourceMetadata {
                CorrelationId = eventStream.CorrelationId,
                SourceId = eventStream.SourceId.Id,
                SourceTypeName = eventStream.SourceId.GetSourceTypeFullName(),
                Version = eventStream.Version
            });

            var collection = eventStream.Events as ICollection;
            if (collection != null) {
                array.AddRange(collection);
            }
            else {
                foreach (var el in eventStream.Events)
                    array.Add(el);
            }

            return array.ToArray();
        }
       
        
    }
}
