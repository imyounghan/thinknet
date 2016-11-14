using System;
using System.Reflection;

namespace ThinkNet.Common.Interception.Pipeline
{
    public struct InterceptorPipelineKey : IEquatable<InterceptorPipelineKey>
    {
        private readonly Module module;
        private readonly int methodMetadataToken;

        private InterceptorPipelineKey(Module module, int methodMetadataToken)
        {
            this.module = module;
            this.methodMetadataToken = methodMetadataToken;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InterceptorPipelineKey))
                return false;

            return this == (InterceptorPipelineKey)obj;
        }

        public override int GetHashCode()
        {
            return this.module.GetHashCode() ^ methodMetadataToken;
        }

        public static bool operator ==(InterceptorPipelineKey left, InterceptorPipelineKey right)
        {
            return left.module == right.module && left.methodMetadataToken == right.methodMetadataToken;
        }

        public static bool operator !=(InterceptorPipelineKey left, InterceptorPipelineKey right)
        {
            return !(left == right);
        }

        #region IEquatable<InterceptorPipelineKey> 成员

        bool IEquatable<InterceptorPipelineKey>.Equals(InterceptorPipelineKey other)
        {
            return this == other;
        }

        #endregion

        public static InterceptorPipelineKey ForMethod(MethodBase method)
        {
            method.NotNull("method");

            return new InterceptorPipelineKey(method.DeclaringType.Module, method.MetadataToken);
        }
    }
}
