using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace ThinkNet.Contracts.Communication
{
    public class WcfClient
    {
        public readonly ConcurrentDictionary<string, IChannelFactory> channelFactories;

        public TService CreateService<TService>()
        {
            return default(TService);
        }

    }
}
