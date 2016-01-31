using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;

namespace ThinkNet.Messaging
{
    [RegisterComponent(typeof(IEventBus))]
    public class EventBus : AbstractBus, IEventBus
    {
        private readonly IMessageSender messageSender;
        private readonly IRoutingKeyProvider routingKeyProvider;
        private readonly IMetadataProvider metadataProvider;
        private readonly ILogger logger;

        public EventBus(IMessageSender messageSender,
            IRoutingKeyProvider routingKeyProvider,
            IMetadataProvider metadataProvider)
        {
            this.messageSender = messageSender;
            this.routingKeyProvider = routingKeyProvider;
            this.metadataProvider = metadataProvider;
            this.logger = LogManager.GetLogger("ThinkNet");
        }

        protected override bool SearchMatchType(Type type)
        {
            return TypeHelper.IsEvent(type);
        }


        public void Publish(IEvent @event)
        {
            this.Publish(new[] { @event });
        }

        public void Publish(IEnumerable<IEvent> events)
        {
            var messages = events.Select(Map).AsEnumerable();
            messageSender.SendAsync(messages, () => {
                if (logger.IsDebugEnabled) {
                    var list = new List<IEvent>();
                    events.ForEach(@event => {
                        var stream = @event as EventStream;
                        if (stream != null)
                            list.AddRange(stream.Events);
                        else
                            list.Add(@event);
                    });

                    logger.DebugFormat("event published. events:{0}", Serialize(list));
                }
            }, (ex) => {
            });
        }

        private Message Map(IEvent @event)
        {
            return new Message {
                Body = @event,
                MetadataInfo = metadataProvider.GetMetadata(@event),
                RoutingKey = routingKeyProvider.GetRoutingKey(@event),
                CreatedTime = DateTime.UtcNow
            };
        }
    }
}