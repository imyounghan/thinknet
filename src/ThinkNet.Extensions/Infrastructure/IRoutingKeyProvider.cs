using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Infrastructure
{
    public interface IRoutingKeyProvider
    {
        string GetRoutingKey<T>(T payload);
    }
}
