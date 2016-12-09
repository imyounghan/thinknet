using System;
using System.Configuration;
using System.Linq;

namespace ThinkNet.Runtime
{
    public class KafkaSettings
    {
        public static readonly KafkaSettings Current = new KafkaSettings();

        private KafkaSettings()
        {
            this.EnsureTopicRetrycount = 5;
            this.EnsureTopicRetryInterval = 1000;

           // this.KafkaUris = ConfigurationManager.AppSettings["thinkcfg.kafka_uri"].IfEmpty(string.Empty).Split(',').Select(str => new Uri(str)).ToArray();
            this.SubscriptionTopics = ConfigurationManager.AppSettings["thinkcfg.kafka_topic"].IfEmpty(string.Empty).Split(',');
            this.ZookeeperAddress = ConfigurationManager.AppSettings["thinkcfg.zookeeper_address"];
            this.BufferCapacity = ConfigurationManager.AppSettings["thinkcfg.kafka_buffer"].ChangeIfError(2000);
        }


        public Uri[] KafkaUris { get; set; }

        public string ZookeeperAddress { get; set; }

        /// <summary>
        /// 订阅的Topic用于消费
        /// </summary>
        public string[] SubscriptionTopics { get; set; }

        ///// <summary>
        ///// 初始化的Topic
        ///// </summary>
        //public string[] InitializationTopics { get; set; }

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

        /// <summary>
        /// 获取或设置从mq拉取消息的缓冲容量
        /// 默认为2000
        /// </summary>
        public int BufferCapacity { get; set; }
    }
}
