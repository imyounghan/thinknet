using System.Collections.Generic;
using System.Reflection;

namespace ThinkNet.Common.Interception
{
    public interface IInterceptorProvider
    {
        IEnumerable<IInterceptor> GetInterceptors(MethodInfo method);
    }
}
