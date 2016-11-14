using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThinkNet.Common.Interception
{
    public class MethodReturn : IMethodReturn
    {
        public MethodReturn(IMethodInvocation originalInvocation, object returnValue, object[] arguments)
        {
            originalInvocation.NotNull("originalInvocation");

            this.InvocationContext = originalInvocation.InvocationContext;
            this.ReturnValue = returnValue;
            this.Outputs = new ParameterCollection(arguments, originalInvocation.TargetMethod.GetParameters(),
                delegate (ParameterInfo pi) { return pi.ParameterType.IsByRef; });
        }

        public MethodReturn(IMethodInvocation originalInvocation, Exception exception)
        {
            originalInvocation.NotNull("originalInvocation");
            exception.NotNull("exception");

            this.InvocationContext = originalInvocation.InvocationContext;
            this.Exception = exception;
            this.Outputs = new ParameterCollection(new object[0], new ParameterInfo[0], delegate { return false; });
        }

        public Exception Exception { get; private set; }

        public IDictionary<string, object> InvocationContext { get; private set; }

        public IParameterCollection Outputs { get; private set; }

        public object ReturnValue { get; private set; }
    }
}
