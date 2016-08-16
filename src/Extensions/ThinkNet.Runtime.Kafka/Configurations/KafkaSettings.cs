using System;
using System.Configuration;
using System.Linq;

namespace ThinkNet.Configurations
{
    public class KafkaSettings
    {
        public static readonly KafkaSettings Current = new KafkaSettings();

        private KafkaSettings()
        {
            this.EnsureTopicRetrycount = 5;
            this.EnsureTopicRetryInterval = 1000;
            this.EnableKafkaProcessor = true;

            this.KafkaUris = (ConfigurationManager.AppSettings["thinkcfg.kafka_uri"] ?? string.Empty).Split(',').Select(str => new Uri(string.Concat("tcp://", str))).ToArray();
            this.Topics = ConfigurationManager.AppSettings["thinkcfg.kafka_topic"].IfEmpty(string.Empty).Split(',');
        }


        public Uri[] KafkaUris { get; set; }

        public string[] Topics { get; set; }

        /// <summary>
        /// 确认Topic遇到错误的重试次数
        /// 默认5次
        /// </summary>
        public int EnsureTopicRetrycount { get; set; }

        /// <summary>
        /// 确认Topic过程中遇到错误等待下次执行的间隔时间（毫秒）
        /// 默认1000ms
        /// </summary>
        public int EnsureTopicRetryInterval { get; set; }

        public bool EnableKafkaProcessor { get; set; }

        public bool EnableCommandReplyProcessor { get; set; }
    }
}
