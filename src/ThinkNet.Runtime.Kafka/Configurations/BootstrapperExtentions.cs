using System;
using System.Linq;
using System.Threading;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Processing;

namespace ThinkNet.Configurations
{
    public static class BootstrapperExtentions
    {
        public static Bootstrapper UsingKafka(this Bootstrapper that)
        {
            that.Register<ITopicProvider, DefaultTopicProvider>();
            that.Register<IEnvelopeSender, KafkaService>();
            that.Register<IEnvelopeReceiver, KafkaService>();
            that.Register<IProcessor, KafkaService>("KafkaProcessor");

            using (var router = new BrokerRouter(new KafkaOptions(KafkaSettings.Current.KafkaUris))) {
                int count = -1;
                while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
                    try {
                        var result = router.GetTopicMetadata(KafkaSettings.Current.SubscriptionTopics);
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
