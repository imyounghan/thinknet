using System;
using ThinkNet.Common.Composition;

namespace ThinkNet.Common.Interception
{
    public abstract class InterceptorAttribute : Attribute
    {
        public int Order { get; set; }

        public abstract IInterceptor CreateInterceptor(IObjectContainer container);
    }
}
