using System;
using System.IO;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using ThinkNet.Runtime;
using ThinkNet.Runtime.Kafka;
using ThinkNet.Runtime.Routing;

namespace ThinkNet
{
    public static class BootstrapperExtentions
    {
        public static Bootstrapper UsingKafka(this Bootstrapper that)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string binPath = string.IsNullOrEmpty(relativeSearchPath) ? baseDir : Path.Combine(baseDir, relativeSearchPath);
            string log4NetConfigFile = string.IsNullOrEmpty(binPath) ? "log4net.config" : Path.Combine(binPath, "log4net.config");

            if(File.Exists(log4NetConfigFile)) {
                XmlConfigurator.ConfigureAndWatch(new FileInfo(log4NetConfigFile));
            }
            else {
                BasicConfigurator.Configure(new ConsoleAppender { Layout = new PatternLayout() });
            }

            that.SetDefault<ITopicProvider, DefaultTopicProvider>();
            that.SetDefault<IEnvelopeSender, KafkaService>();
            that.SetDefault<IEnvelopeReceiver, KafkaService>();
            that.SetDefault<IProcessor, KafkaService>("kafka");

            //Console.WriteLine("kafka:{0}", );

            //using (var router = new BrokerRouter(new KafkaOptions(KafkaSettings.Current.KafkaUris))) {
            //    int count = -1;
            //    while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
            //        try {
            //            var result = router.GetTopicMetadata(KafkaSettings.Current.SubscriptionTopics);
            //            if (result.All(topic => topic.ErrorCode == (short)ErrorResponseCode.NoError))
            //                break;

            //            result.Where(topic => topic.ErrorCode != (short)ErrorResponseCode.NoError)
            //                .ForEach(topic => {
            //                    if (LogManager.Default.IsWarnEnabled)
            //                        LogManager.Default.WarnFormat("get the topic('{0}') of status is {1}.",
            //                            topic.Name, (ErrorResponseCode)topic.ErrorCode);
            //                });


            //            Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
            //        }
            //        catch (Exception) {
            //            //TODO...Write LOG
            //            throw;
            //        }
            //    }
            //}

            return that;
        }
    }
}
