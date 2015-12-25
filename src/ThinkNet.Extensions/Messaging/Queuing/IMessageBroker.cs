using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Messaging.Queuing
{
    public interface IMessageBroker
    {
        bool TryAdd(MetaMessage message);

        bool TryGet(out MetaMessage message);

        MetaMessage Take();
    }
}
