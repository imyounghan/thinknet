using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling
{
    public class EventHandlerWrapper : MessageHandlerWrapper
    {
        public EventHandlerWrapper(IHandler handler, Type contractType)
            : base(handler, contractType)
        { }

        private object GetParameter(object[] args, Type type)
        {
            return args.Skip(1).First(p => p.GetType() == type);
        }

        protected override MethodInfo GetHandleMethodInfo(Type targetType, Type[] parameterTypes)
        {
            var types = new List<Type>(parameterTypes);
            types.Insert(0, typeof(VersionData));

            return targetType.GetMethod("Handle", types.ToArray());
        }

        public override void Handle(object handler, object[] args)
        {
            var parames = new ArrayList();
            parames.Add(args[0]);

            switch (args.Length - 1) {
                case 1:
                    //((dynamic)handler).Handle((dynamic)version, (dynamic)args[1]);
                    parames.Add(args[1]);
                    break;
                case 2:
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[1]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[2]));
                    //var event1 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[1]);
                    //var event2 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[2]);        
                    //((dynamic)handler).Handle((dynamic)version, (dynamic)event1, (dynamic)event2);
                    break;
                case 3:
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[1]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[2]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[3]));
                    //event1 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[1]);
                    //event2 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[2]);
                    //var event3 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[3]);
                    //((dynamic)handler).Handle((dynamic)version, (dynamic)event1, (dynamic)event2, (dynamic)event3);
                    break;
                case 4:
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[1]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[2]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[3]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[4]));
                    //event1 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[1]);
                    //event2 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[2]);
                    //event3 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[3]);
                    //var event4 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[4]);
                    //((dynamic)handler).Handle((dynamic)version, (dynamic)event1, (dynamic)event2, (dynamic)event3, (dynamic)event4);
                    break;
                case 5:
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[1]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[2]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[3]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[4]));
                    parames.Add(GetParameter(args, ContractType.GenericTypeArguments[5]));
                    //event1 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[1]);
                    //event2 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[2]);
                    //event3 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[3]);
                    //event4 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[4]);
                    //var event5 = args.First(p => p.GetType() == ContractType.GenericTypeArguments[5]);
                    //((dynamic)handler).Handle((dynamic)version, (dynamic)event1, (dynamic)event2, (dynamic)event3, (dynamic)event4, (dynamic)event5);
                    break;
            }

            
            InterceptorPipeline pipeline;
            var method = GetHandleMethodInfo(out pipeline);
            var parameterValues = parames.ToArray();
            method.Invoke(handler, parameterValues);

            //if (pipeline.Count == 0) {
            //    method.Invoke(handler, parameterValues);
            //}
            //else {
            //    var input = new MethodInvocation(handler, method, parameterValues);
            //    pipeline.Invoke(input, delegate {
            //        method.Invoke(handler, parameterValues);
            //        return new MethodReturn(input, null, parameterValues);
            //    });
            //}
            
        }
    }
}
