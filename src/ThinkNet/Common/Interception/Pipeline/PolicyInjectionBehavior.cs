using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ThinkNet.Common.Interception.Pipeline
{
    public class PolicyInjectionBehavior
    {
        public IMethodReturn Invode(IMethodInvocation input, GetNextInterceptorDelegate getNext)
        {
            var pipeline = GetPipeline(input.MethodBase);

            var result = pipeline.Invoke(input, delegate(IMethodInvocation injectionInput, GetNextInterceptorDelegate injectionGetNext) {
                return getNext()(injectionInput, injectionGetNext);
            });

            return result;
        }

        private InterceptorPipeline GetPipeline(MethodBase method)
        {
            return null;
        }
    }
}
