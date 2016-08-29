using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Messaging
{
    public class Envelope
    {
        public string Body { get; private set; }

        public Type Type { get; private set; }

        public string CorrelationId { get; private set; }

        public string RoutingKey { get; private set; }

        public string Kind { get; private set; }
    }
}
