using System.Collections.Generic;
using System.Reflection;

namespace ThinkNet.Common.Interception.Pipeline
{
    public class InterceptorPipelineManager
    {
        private static readonly InterceptorPipeline EmptyPipeline = new InterceptorPipeline();

        private readonly Dictionary<InterceptorPipelineKey, InterceptorPipeline> pipelines = new Dictionary<InterceptorPipelineKey, InterceptorPipeline>();

        public InterceptorPipeline GetPipeline(MethodBase method)
        {
            var key = InterceptorPipelineKey.ForMethod(method);
            if (pipelines.ContainsKey(key))
                return pipelines[key];

            return EmptyPipeline;
        }

        public void SetPipeline(MethodBase method, InterceptorPipeline pipeline)
        {
            var key = InterceptorPipelineKey.ForMethod(method);
            pipelines[key] = pipeline;
        }

        //public bool InitializePipeline(methodi)
        //{
        //}

        public InterceptorPipeline CreatePipeline(MethodInfo method, IEnumerable<IInterceptor> interceptors)
        {
            var key = InterceptorPipelineKey.ForMethod(method);

            if (pipelines.ContainsKey(key))
                return pipelines[key];

            if (method.GetBaseDefinition() == method)
                return pipelines[key] = new InterceptorPipeline(interceptors);

            return pipelines[key] = CreatePipeline(method.GetBaseDefinition(), interceptors);
        }
    }
}
