using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 处理事件的代理程序
    /// </summary>
    public class EventHandlerAgent : MessageHandlerAgent
    {
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventHandlerAgent(object handler, MethodInfo method)
            : base(handler, method, null)
        { }

        /// <summary>
        /// 处理一批事件
        /// </summary>
        public override void Handle(object[] args)
        {
            var parames = new ArrayList();
            parames.Add(args[0]);

            var parameters = ReflectedMethod.GetParameters();
            switch(args.Length - 1) {
                case 1:
                    parames.Add(args[1]);
                    break;
                case 2:
                    parames.Add(GetParameter(args, parameters[1].ParameterType));
                    parames.Add(GetParameter(args, parameters[2].ParameterType));
                    break;
                case 3:
                    parames.Add(GetParameter(args, parameters[1].ParameterType));
                    parames.Add(GetParameter(args, parameters[2].ParameterType));
                    parames.Add(GetParameter(args, parameters[3].ParameterType));
                    break;
                case 4:
                    parames.Add(GetParameter(args, parameters[1].ParameterType));
                    parames.Add(GetParameter(args, parameters[2].ParameterType));
                    parames.Add(GetParameter(args, parameters[3].ParameterType));
                    parames.Add(GetParameter(args, parameters[4].ParameterType));
                    break;
                case 5:
                    parames.Add(GetParameter(args, parameters[1].ParameterType));
                    parames.Add(GetParameter(args, parameters[2].ParameterType));
                    parames.Add(GetParameter(args, parameters[3].ParameterType));
                    parames.Add(GetParameter(args, parameters[4].ParameterType));
                    parames.Add(GetParameter(args, parameters[5].ParameterType));
                    break;
                default:
                    throw new ThinkNetException("Unknow");
            }


            base.Handle(parames.ToArray());
        }

        private object GetParameter(object[] args, Type type)
        {
            return args.Skip(1).First(p => p.GetType() == type);
        }
    }

    public class EventHandlerAgent<TEvent> : HandlerAgent
        where TEvent : Event
    {
        public EventHandlerAgent(IEventHandler<TEvent> handler)
            : base(handler)
        { }

        protected override void TryHandle(object[] args)
        {
            var eventHandler = GetTargetHandler() as IEventHandler<TEvent>;
            eventHandler.Handle(args[0] as SourceMetadata, args[1] as TEvent);
        }
    }

    public class EventHandlerAgent<TEvent1, TEvent2> : HandlerAgent
        where TEvent1 : Event
        where TEvent2 : Event
    {
        public EventHandlerAgent(IEventHandler<TEvent1, TEvent2> handler)
            : base(handler)
        { }

        protected override void TryHandle(object[] args)
        {
            var eventHandler = GetTargetHandler() as IEventHandler<TEvent1, TEvent2>;
            eventHandler.Handle(args[0] as SourceMetadata,
                GetValue<TEvent1>(args.Skip(1)),
                GetValue<TEvent2>(args.Skip(1)));
        }
    }

    public class EventHandlerAgent<TEvent1, TEvent2, TEvent3> : HandlerAgent
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
    {
        public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3> handler)
            : base(handler)
        { }

        protected override void TryHandle(object[] args)
        {
            var eventHandler = GetTargetHandler() as IEventHandler<TEvent1, TEvent2, TEvent3>;
            eventHandler.Handle(args[0] as SourceMetadata,
                GetValue<TEvent1>(args.Skip(1)),
                GetValue<TEvent2>(args.Skip(1)),
                GetValue<TEvent3>(args.Skip(1)));
        }
    }

    public class EventHandlerAgent<TEvent1, TEvent2, TEvent3, TEvent4> : HandlerAgent
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
        where TEvent4 : Event
    {
        public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4> handler)
            : base(handler)
        { }

        protected override void TryHandle(object[] args)
        {
            var eventHandler = GetTargetHandler() as IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4>;
            eventHandler.Handle(args[0] as SourceMetadata,
                GetValue<TEvent1>(args.Skip(1)),
                GetValue<TEvent2>(args.Skip(1)),
                GetValue<TEvent3>(args.Skip(1)),
                GetValue<TEvent4>(args.Skip(1)));
        }
    }

    public class EventHandlerAgent<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> : HandlerAgent
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
        where TEvent4 : Event
        where TEvent5 : Event
    {
        public EventHandlerAgent(IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> handler)
            : base(handler)
        { }

        protected override void TryHandle(object[] args)
        {
            var eventHandler = GetTargetHandler() as IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>;
            eventHandler.Handle(args[0] as SourceMetadata,
                GetValue<TEvent1>(args.Skip(1)),
                GetValue<TEvent2>(args.Skip(1)),
                GetValue<TEvent3>(args.Skip(1)),
                GetValue<TEvent4>(args.Skip(1)),
                GetValue<TEvent5>(args.Skip(1)));
        }
    }
}
