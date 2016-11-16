using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Proxies
{
    public class EventHandlerProxy : MessageHandlerProxy
    {
        public EventHandlerProxy(IHandler handler, MethodInfo method, InterceptorPipeline pipeline)
            : base(handler, method, pipeline)
        { }

        private object GetParameter(object[] args, Type type)
        {
            return args.Skip(1).First(p => p.GetType() == type);
        }
        
        public override void Handle(object[] args)
        {
            var parames = new ArrayList();
            parames.Add(args[0]);

            var parameters = Method.GetParameters();
            switch (args.Length - 1) {
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
            }


            base.Handle(parames.ToArray());
        }
    }
}
