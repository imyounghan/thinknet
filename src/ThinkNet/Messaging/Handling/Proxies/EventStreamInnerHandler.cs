using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Common;
using ThinkNet.Common.Composition;
using ThinkNet.Contracts;
using ThinkNet.Domain;

namespace ThinkNet.Messaging.Handling.Proxies
{
    /// <summary>
    /// <see cref="EventStream"/> 的内部处理程序
    /// </summary>
    public class EventStreamInnerHandler : MessageHandlerProxy
    {
        private readonly IHandlerMethodProvider _handlerMethodProvider;
        
        private readonly ConcurrentDictionary<Type, IHandlerProxy> _cachedHandlers;
        private readonly Lazy<MethodInfo> _method;
        

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventStreamInnerHandler(IHandlerMethodProvider handlerMethodProvider)
            : base(null)
        {
            this._handlerMethodProvider = handlerMethodProvider;
            this._method = new Lazy<MethodInfo>(GetMethodInfo);
            this._cachedHandlers = new ConcurrentDictionary<Type, IHandlerProxy>();
            this.HandlerInstance = this;
        }

        private static MethodInfo GetMethodInfo()
        {
            return typeof(EventStreamInnerHandler).GetMethod("Handle", new[] { typeof(EventStream) });
        }

        /// <summary>
        /// 处理 <see cref="EventStream"/> 的反射方法
        /// </summary>
        public override MethodInfo ReflectedMethod { get { return _method.Value; } }

        ///// <summary>
        ///// 处理 <see cref="EventStream"/> 的实例
        ///// </summary>
        //public object HandlerInstance { get { return this; } }

        ///// <summary>
        ///// 处理数据
        ///// </summary>
        //public virtual void Handle(params object[] args)
        //{
        //    var eventStream = args[0] as EventStream;
        //    if (eventStream == null) {
        //        //TODO..
        //        return;
        //    }

        //    if (_handlerStore.HandlerIsExecuted(eventStream.CorrelationId, _messageType, _handlerType)) {
        //        //var errorMessage = string.Format("The EventStream has been handled. AggregateRootType:{0}, AggregateRootId:{1}, CommandId:{2}.",
        //        //    eventStream.SourceType.FullName, eventStream.SourceId, eventStream.CorrelationId);

        //        if (LogManager.Default.IsWarnEnabled)
        //            LogManager.Default.WarnFormat("The EventStream has been handled. AggregateRootType:{0}, AggregateRootId:{1}, CommandId:{2}.",
        //                eventStream.SourceType.FullName, eventStream.SourceId, eventStream.CorrelationId);
        //        //throw new ThinkNetException(errorMessage);
        //        return;
        //    }

        //    this.Handle(eventStream);

        //    _handlerStore.AddHandlerInfo(eventStream.CorrelationId, _messageType, _handlerType);
        //}


        //private CommandResultReplied Transform(EventStream @event, Exception ex)
        //{
        //    var reply = @event.Events.IsEmpty() ? 
        //        new CommandResultReplied(@event.CorrelationId, CommandReturnType.DomainEventHandled, CommandStatus.NothingChanged) :
        //        new CommandResultReplied(@event.CorrelationId, CommandReturnType.DomainEventHandled, ex);

        //    return reply;
        //}

        protected override void TryHandleWithoutPipeline(object[] args)
        {
            var eventStream = args[0] as EventStream;

            this.Handle(eventStream);
        }

        private void Handle(EventStream @event)
        {
            var eventTypes = @event.Events.Select(p => p.GetType()).ToArray();
            GetEventHandler(eventTypes).Handle(this.GetParameter(@event));

            //try {
            //    GetEventHandler(eventTypes).Handle(this.GetParameter(@event));
            //    _bus.Publish(@event.Events);
            //}
            //catch(DomainEventAsPendingException) {
            //    _bus.Publish(@event);
            //}
            //catch(Exception) {
            //    throw;
            //}
            //finally {
            //}
        }

        private IHandlerProxy BuildEventHandler(object handler, Type contractType)
        {
            var method = _handlerMethodProvider.GetCachedMethodInfo(handler.GetType(), contractType);
            return new EventHandlerProxy(handler, method);
        }

        /// <summary>
        /// 获取处理事件的代理
        /// </summary>
        protected IHandlerProxy GetEventHandler(Type[] types)
        {
            var contractType = HandlerFetchedProvider.Instance.GetEventHandlerType(types);

            IHandlerProxy cachedHandler;
            if(_cachedHandlers.TryGetValue(contractType, out cachedHandler))
                return cachedHandler;
            
            var handlers = HandlerFetchedProvider.Instance.GetEventHandlers(contractType)
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
        protected object[] GetParameter(EventStream @event)
        {
            var array = new ArrayList();
            array.Add(new SourceDataKey {
                CorrelationId = @event.CorrelationId,
                SourceId = @event.SourceId,
                SourceType = @event.SourceType,
                Version = @event.Version
            });

            var collection = @event.Events as ICollection;
            if (collection != null) {
                array.AddRange(collection);
            }
            else {
                foreach (var el in @event.Events)
                    array.Add(el);
            }

            return array.ToArray();
        }
       
        
    }
}
