using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace ThinkNet.Messaging.Handling.Agent
{
    ///// <summary>
    ///// 处理事件的代理程序
    ///// </summary>
    //public class EventHandlerAgent : MessageHandlerAgent
    //{
    //    /// <summary>
    //    /// Parameterized Constructor.
    //    /// </summary>
    //    public EventHandlerAgent(object handler, MethodInfo method)
    //        : base(handler, method, null)
    //    { }

    //    /// <summary>
    //    /// 处理一批事件
    //    /// </summary>
    //    public override void Handle(object[] args)
    //    {
    //        var parames = new ArrayList();
    //        parames.Add(args[0]);

    //        var parameters = ReflectedMethod.GetParameters();
    //        switch(args.Length - 1) {
    //            case 1:
    //                parames.Add(args[1]);
    //                break;
    //            case 2:
    //                parames.Add(GetParameter(args, parameters[1].ParameterType));
    //                parames.Add(GetParameter(args, parameters[2].ParameterType));
    //                break;
    //            case 3:
    //                parames.Add(GetParameter(args, parameters[1].ParameterType));
    //                parames.Add(GetParameter(args, parameters[2].ParameterType));
    //                parames.Add(GetParameter(args, parameters[3].ParameterType));
    //                break;
    //            case 4:
    //                parames.Add(GetParameter(args, parameters[1].ParameterType));
    //                parames.Add(GetParameter(args, parameters[2].ParameterType));
    //                parames.Add(GetParameter(args, parameters[3].ParameterType));
    //                parames.Add(GetParameter(args, parameters[4].ParameterType));
    //                break;
    //            case 5:
    //                parames.Add(GetParameter(args, parameters[1].ParameterType));
    //                parames.Add(GetParameter(args, parameters[2].ParameterType));
    //                parames.Add(GetParameter(args, parameters[3].ParameterType));
    //                parames.Add(GetParameter(args, parameters[4].ParameterType));
    //                parames.Add(GetParameter(args, parameters[5].ParameterType));
    //                break;
    //            default:
    //                throw new ThinkNetException("Unknow");
    //        }


    //        base.Handle(parames.ToArray());
    //    }

    //    private object GetParameter(object[] args, Type type)
    //    {
    //        return args.Skip(1).First(p => p.GetType() == type);
    //    }
    //}

    public class EventHandlerAgent<TEvent> : HandlerAgent
        where TEvent : Event
    {
        private readonly IEventHandler<TEvent> _targetHandler;
        private readonly Type _contractType;
        public EventHandlerAgent(IEventHandler<TEvent> handler)
            : base(null)
        {
            this._targetHandler = handler;
            this._contractType = typeof(IEventHandler<TEvent>);
        }

        protected override void TryHandle(object[] args)
        {
            _targetHandler.Handle(args[0] as SourceMetadata, args[1] as TEvent);
        }

        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }

        protected override Type GetHandlerInterfaceType()
        {
            return this._contractType;
        }
    }

    public class EventHandlerAgent<TEvent1, TEvent2> : HandlerAgent
        where TEvent1 : Event
        where TEvent2 : Event
    {
        private readonly IEventHandler<TEvent1, TEvent2> _targetHandler;
        private readonly Type _contractType;

        public EventHandlerAgent(IEventHandler<TEvent1, TEvent2> handler)
            : base(null)
        { 
            this._targetHandler = handler;
            this._contractType = typeof(IEventHandler<TEvent1, TEvent2>);
        }

        protected override void TryHandle(object[] args)
        {
            _targetHandler.Handle(args[0] as SourceMetadata, 
                args[1] as TEvent1,
                args[2] as TEvent2);
        }

        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }

        protected override Type GetHandlerInterfaceType()
        {
            return this._contractType;
        }
    }

    public class EventHandlerAgent<TEvent1, TEvent2, TEvent3> : HandlerAgent
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
    {
        private readonly IEventHandler<TEvent1, TEvent2, TEvent3> _targetHandler;
        private readonly Type _contractType;
        public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3> handler)
            : base(null)
        {
            this._targetHandler = handler;
            this._contractType = typeof(IEventHandler<TEvent1, TEvent2, TEvent3>);
        }

        protected override void TryHandle(object[] args)
        {
            _targetHandler.Handle(args[0] as SourceMetadata,
                args[1] as TEvent1,
                args[2] as TEvent2,
                args[3] as TEvent3);
        }

        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }

        protected override Type GetHandlerInterfaceType()
        {
            return this._contractType;
        }
    }

    public class EventHandlerAgent<TEvent1, TEvent2, TEvent3, TEvent4> : HandlerAgent
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
        where TEvent4 : Event
    {
        private readonly IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4> _targetHandler;
        private readonly Type _contractType;
        public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4> handler)
            : base(null)
        {
            this._targetHandler = handler;
            this._contractType = typeof(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4>);
        }

        protected override void TryHandle(object[] args)
        {
            _targetHandler.Handle(args[0] as SourceMetadata,
                args[1] as TEvent1,
                args[2] as TEvent2,
                args[3] as TEvent3,
                args[4] as TEvent4);
        }

        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }

        protected override Type GetHandlerInterfaceType()
        {
            return this._contractType;
        }
    }

    public class EventHandlerAgent<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> : HandlerAgent
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
        where TEvent4 : Event
        where TEvent5 : Event
    {
        private readonly IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _targetHandler;
        private readonly Type _contractType;
        public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> handler)
            : base(null)
        {
            this._targetHandler = handler;
            this._contractType = typeof(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>);
        }

        protected override void TryHandle(object[] args)
        {
            _targetHandler.Handle(args[0] as SourceMetadata,
                args[1] as TEvent1,
                args[2] as TEvent2,
                args[3] as TEvent3,
                args[4] as TEvent4,
                args[5] as TEvent5);
        }

        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }

        protected override Type GetHandlerInterfaceType()
        {
            return this._contractType;
        }
    }
}
