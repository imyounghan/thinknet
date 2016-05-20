using System;
using System.Collections.Generic;

namespace ThinkNet.Configurations
{
    public class ConfigurationSetting
    {
        public static readonly ConfigurationSetting Current = new ConfigurationSetting();

        private ConfigurationSetting()
        {
            this.HandleRetrytimes = 5;
            this.HandleRetryInterval = 1000;
            this.QueueCount = 4;
        }

        /// <summary>
        /// 消息处理器运行过程中遇到错误的重试次数
        /// 默认5次
        /// </summary>
        public int HandleRetrytimes { get; set; }

        /// <summary>
        /// 消息处理器运行过程中遇到错误等待下次执行的间隔时间（毫秒）
        /// 默认1000ms
        /// </summary>
        public int HandleRetryInterval { get; set; }

        /// <summary>
        /// 内部消息队列的个数，建议和CPU核数一样
        /// 默认为4
        /// </summary>
        public int QueueCount { get; set; }
    }
}
