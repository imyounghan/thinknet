using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 处理事件的代理程序
    /// </summary>
    public class EventHandlerAgent : HandlerAgent
    {
        private readonly IHandler _targetHandler;
        private readonly Type _contractType;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventHandlerAgent(Type eventHandlerInterfaceType, IHandler handler)
        {
            this._targetHandler = handler;
            this._contractType = eventHandlerInterfaceType;
        }
        
        /// <summary>
        /// 获取事件处理程序
        /// </summary>
        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }

        /// <summary>
        /// 处理一批事件
        /// </summary>
        public override void Handle(object[] args)
        {
            //var parameters = new ArrayList();
            //parameters.Add(args[0]);
            //List<object> list = new List<object>(args.Skip(1));

            var parameterTypes = _contractType.GetGenericArguments();
            //switch (parameterTypes.Length) {
            //    case 1:
            //        parameters.Add(args[1]);
            //        break;
            //    case 2:
            //        parameters.Add(GetParameter(list, parameterTypes[0]));
            //        parameters.Add(GetParameter(list, parameterTypes[1]));
            //        break;
            //    case 3:
            //        parameters.Add(GetParameter(list, parameterTypes[0]));
            //        parameters.Add(GetParameter(list, parameterTypes[1]));
            //        parameters.Add(GetParameter(list, parameterTypes[2]));
            //        break;
            //    case 4:
            //        parameters.Add(GetParameter(list, parameterTypes[0]));
            //        parameters.Add(GetParameter(list, parameterTypes[1]));
            //        parameters.Add(GetParameter(list, parameterTypes[2]));
            //        parameters.Add(GetParameter(list, parameterTypes[3]));
            //        break;
            //    case 5:
            //        parameters.Add(GetParameter(list, parameterTypes[0]));
            //        parameters.Add(GetParameter(list, parameterTypes[1]));
            //        parameters.Add(GetParameter(list, parameterTypes[2]));
            //        parameters.Add(GetParameter(list, parameterTypes[3]));
            //        parameters.Add(GetParameter(list, parameterTypes[4]));
            //        break;
            //    default:
            //        throw new ThinkNetException("Unknow");
            //}
            //var parameters = GetParameters(new List<object>(args));
            //base.Handle(parameters);

            object[] parameters = args;
            if (parameterTypes.Length > 1) {
                parameters = GetParameters(args, parameterTypes);
            }

            base.Handle(parameters);
        }

        /// <summary>
        /// 尝试处理事件
        /// </summary>
        protected override void TryHandle(object[] args)
        {
            switch(args.Length - 1) {
                case 1:
                    ((dynamic)_targetHandler).Handle((dynamic)args[0], (dynamic)args[1]);
                    break;
                case 2:
                    ((dynamic)_targetHandler).Handle((dynamic)args[0], (dynamic)args[1], (dynamic)args[2]);
                    break;
                case 3:
                    ((dynamic)_targetHandler).Handle((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3]);
                    break;
                case 4:
                    ((dynamic)_targetHandler).Handle((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4]);
                    break;
                case 5:
                    ((dynamic)_targetHandler).Handle((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5]);
                    break;
                default:
                    throw new ThinkNetException("Unknow");
            }
        }

        private object[] GetParameters(IEnumerable<object> args, IEnumerable<Type> parameterTypes)
        {
            List<object> list = new List<object>(args);
            var parameters = new ArrayList();
            parameters.Add(list[0]);
            list.RemoveAt(0);            
            foreach (var parameterType in parameterTypes) {
                var result = list.First(p => p.GetType() == parameterType);
                list.Remove(result);
                parameters.Add(result);
            }

            return parameters.ToArray();
        }

        //private object GetParameter(List<object> args, Type type)
        //{
        //    var result = args.Skip(1).First(p => p.GetType() == type);
        //    args.Remove(result);

        //    return result;
        //}
    }

    //public class EventHandlerAgent<TEvent> : HandlerAgent
    //    where TEvent : Event
    //{
    //    private readonly IEventHandler<TEvent> _targetHandler;
    //    private readonly Type _contractType;
    //    public EventHandlerAgent(IEventHandler<TEvent> handler)
    //        : base(null)
    //    {
    //        this._targetHandler = handler;
    //        this._contractType = typeof(IEventHandler<TEvent>);
    //    }

    //    protected override void TryHandle(object[] args)
    //    {
    //        _targetHandler.Handle(args[0] as SourceMetadata, args[1] as TEvent);
    //    }

    //    public override object GetInnerHandler()
    //    {
    //        return this._targetHandler;
    //    }

    //    protected override Type GetHandlerInterfaceType()
    //    {
    //        return this._contractType;
    //    }
    //}

    //public class EventHandlerAgent<TEvent1, TEvent2> : HandlerAgent
    //    where TEvent1 : Event
    //    where TEvent2 : Event
    //{
    //    private readonly IEventHandler<TEvent1, TEvent2> _targetHandler;
    //    private readonly Type _contractType;

    //    public EventHandlerAgent(IEventHandler<TEvent1, TEvent2> handler)
    //        : base(null)
    //    { 
    //        this._targetHandler = handler;
    //        this._contractType = typeof(IEventHandler<TEvent1, TEvent2>);
    //    }

    //    protected override void TryHandle(object[] args)
    //    {
    //        _targetHandler.Handle(args[0] as SourceMetadata, 
    //            args[1] as TEvent1,
    //            args[2] as TEvent2);
    //    }

    //    public override object GetInnerHandler()
    //    {
    //        return this._targetHandler;
    //    }

    //    protected override Type GetHandlerInterfaceType()
    //    {
    //        return this._contractType;
    //    }
    //}

    //public class EventHandlerAgent<TEvent1, TEvent2, TEvent3> : HandlerAgent
    //    where TEvent1 : Event
    //    where TEvent2 : Event
    //    where TEvent3 : Event
    //{
    //    private readonly IEventHandler<TEvent1, TEvent2, TEvent3> _targetHandler;
    //    private readonly Type _contractType;
    //    public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3> handler)
    //        : base(null)
    //    {
    //        this._targetHandler = handler;
    //        this._contractType = typeof(IEventHandler<TEvent1, TEvent2, TEvent3>);
    //    }

    //    protected override void TryHandle(object[] args)
    //    {
    //        _targetHandler.Handle(args[0] as SourceMetadata,
    //            args[1] as TEvent1,
    //            args[2] as TEvent2,
    //            args[3] as TEvent3);
    //    }

    //    public override object GetInnerHandler()
    //    {
    //        return this._targetHandler;
    //    }

    //    protected override Type GetHandlerInterfaceType()
    //    {
    //        return this._contractType;
    //    }
    //}

    //public class EventHandlerAgent<TEvent1, TEvent2, TEvent3, TEvent4> : HandlerAgent
    //    where TEvent1 : Event
    //    where TEvent2 : Event
    //    where TEvent3 : Event
    //    where TEvent4 : Event
    //{
    //    private readonly IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4> _targetHandler;
    //    private readonly Type _contractType;
    //    public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4> handler)
    //        : base(null)
    //    {
    //        this._targetHandler = handler;
    //        this._contractType = typeof(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4>);
    //    }

    //    protected override void TryHandle(object[] args)
    //    {
    //        _targetHandler.Handle(args[0] as SourceMetadata,
    //            args[1] as TEvent1,
    //            args[2] as TEvent2,
    //            args[3] as TEvent3,
    //            args[4] as TEvent4);
    //    }

    //    public override object GetInnerHandler()
    //    {
    //        return this._targetHandler;
    //    }

    //    protected override Type GetHandlerInterfaceType()
    //    {
    //        return this._contractType;
    //    }
    //}

    //public class EventHandlerAgent<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> : HandlerAgent
    //    where TEvent1 : Event
    //    where TEvent2 : Event
    //    where TEvent3 : Event
    //    where TEvent4 : Event
    //    where TEvent5 : Event
    //{
    //    private readonly IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _targetHandler;
    //    private readonly Type _contractType;
    //    public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> handler)
    //        : base(null)
    //    {
    //        this._targetHandler = handler;
    //        this._contractType = typeof(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>);
    //    }

    //    protected override void TryHandle(object[] args)
    //    {
    //        _targetHandler.Handle(args[0] as SourceMetadata,
    //            args[1] as TEvent1,
    //            args[2] as TEvent2,
    //            args[3] as TEvent3,
    //            args[4] as TEvent4,
    //            args[5] as TEvent5);
    //    }

    //    public override object GetInnerHandler()
    //    {
    //        return this._targetHandler;
    //    }

    //    protected override Type GetHandlerInterfaceType()
    //    {
    //        return this._contractType;
    //    }
    //}
}
