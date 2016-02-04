using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Messaging.Handling
{
    public interface IInterceptorProvider
    {
        IEnumerable<IProxyInterceptor> GetInterceptors(Type type);
    }
}
