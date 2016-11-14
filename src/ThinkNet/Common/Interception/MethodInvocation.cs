using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThinkNet.Common.Interception
{
    public class MethodInvocation : IMethodInvocation
    {
        public MethodInvocation(object target, MethodBase targetMethod, params object[] parameterValues)
        {
            target.NotNull("target");
            targetMethod.NotNull("targetMethod");

            this.Target = target;
            this.TargetMethod = targetMethod;
            this.InvocationContext = new Dictionary<string, object>();

            ParameterInfo[] targetParameters = targetMethod.GetParameters();
            this.Arguments = new ParameterCollection(parameterValues, targetParameters, param => true);
            this.Inputs = new ParameterCollection(parameterValues, targetParameters, param => !param.IsOut);
        }

        public IParameterCollection Arguments { get; private set; }

        public IParameterCollection Inputs { get; private set; }

        public IDictionary<string, object> InvocationContext { get; private set; }

        public MethodBase TargetMethod { get; private set; }

        public object Target { get; private set; }

        public IMethodReturn CreateExceptionMethodReturn(Exception ex)
        {
            return new MethodReturn(this, ex);
        }

        public IMethodReturn CreateMethodReturn(object returnValue, params object[] outputs)
        {
            return new MethodReturn(this, returnValue, outputs);
        }
    }
}
