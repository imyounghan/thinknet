using System;
using System.Linq;
using System.Threading;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using ThinkNet.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Processing;

namespace ThinkNet.Configurations
{
    public static class BootstrapperExtentions
    {
        public static Bootstrapper UsingKafka(this Bootstrapper that)
        {
            //that.RegisterType<ICommandBus, CommandBus>();
            //that.RegisterType<IEventBus, EventBus>();
            //that.RegisterType<ITopicProvider, DefaultTopicProvider>();
            //if (KafkaSettings.Current.EnableKafkaProcessor)
            //    that.RegisterType<IProcessor, KafkaProcessor>("KafkaProcessor");
            //if (KafkaSettings.Current.EnableCommandReplyProcessor)
            //    that.RegisterType<IProcessor, CommandReplyProcessor>("CommandReplyProcessor");
            //that.RegisterType<ICommandNotification, CommandNotification>();
            //that.RegisterType<IEnvelopeDelivery, EnvelopeDelivery>();
            //that.RegisterType<IEnvelopeHub, EnvelopeHub>();

            using (var router = new BrokerRouter(new KafkaOptions(KafkaSettings.Current.KafkaUris))) {
                int count = -1;
                while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
                    try {
                        var result = router.GetTopicMetadata(KafkaSettings.Current.Topics);
                        if (result.All(topic => topic.ErrorCode == (short)ErrorResponseCode.NoError))
                            break;

                        result.Where(topic => topic.ErrorCode != (short)ErrorResponseCode.NoError)
                            .ForEach(topic => {
                                if (LogManager.Default.IsWarnEnabled)
                                    LogManager.Default.WarnFormat("get the topic('{0}') of status is {1}.",
                                        topic.Name, (ErrorResponseCode)topic.ErrorCode);
                            });


                        Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
                    }
                    catch (Exception) {
                        //TODO...Write LOG
                        throw;
                    }
                }
            }

            return that;
        }
    }
}
