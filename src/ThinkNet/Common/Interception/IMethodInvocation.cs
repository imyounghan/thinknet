using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThinkNet.Common.Interception
{
    public interface IMethodInvocation
    {
        /// <summary>
        /// 获取所有的参数集合
        /// </summary>
        IParameterCollection Arguments { get; }
        /// <summary>
        /// 获取输入的参数集合
        /// </summary>
        IParameterCollection Inputs { get; }

        IDictionary<string, object> InvocationContext { get; }

        MethodBase MethodBase { get; }

        object Target { get; }


        IMethodReturn CreateExceptionMethodReturn(Exception ex);
        IMethodReturn CreateMethodReturn(object returnValue, params object[] outputs);
    }    
}
