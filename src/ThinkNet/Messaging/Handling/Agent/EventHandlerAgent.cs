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
}
