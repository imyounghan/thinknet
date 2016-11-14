using System.Collections.Generic;

namespace ThinkNet.Common.Interception.Pipeline
{
    public class InterceptorPipeline
    {
        private readonly IList<IInterceptor> _interceptors;

        public InterceptorPipeline()
        {
            this._interceptors = new List<IInterceptor>();
        }

        public InterceptorPipeline(IEnumerable<IInterceptor> interceptors)
        {
            this._interceptors = new List<IInterceptor>(interceptors);
        }

        public int Count { get { return _interceptors.Count; } }

        public IMethodReturn Invoke(IMethodInvocation input, InvokeInterceptorDelegate target)
        {
            if (this.Count == 0)
                return target(input, null);

            int index = 0;

            IMethodReturn result = _interceptors[0].Invoke(input, delegate {
                ++index;
                if (index < this.Count) {
                    return _interceptors[index].Invoke;
                }
                else {
                    return target;
                }
            });

            return result;
        }
    }
}
