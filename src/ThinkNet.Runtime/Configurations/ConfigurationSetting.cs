using System.Configuration;

namespace ThinkNet.Configurations
{
    public class ConfigurationSetting
    {
        public static readonly ConfigurationSetting Current = new ConfigurationSetting();


        private ConfigurationSetting()
        {
            this.HandleRetrytimes = 5;
            this.HandleRetryInterval = 1000;
            this.QueueCount = ConfigurationManager.AppSettings["thinkcfg.queue_count"].ChangeIfError(4);
            this.QueueCapacity = ConfigurationManager.AppSettings["thinkcfg.queue_capacity"].ChangeIfError(1000);
            //this.EnableCommandProcessor = true;
            //this.EnableEventProcessor = true;
            //this.EnableSynchronousProcessor = true;
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
        /// 内部消息的队列数量
        /// 默认为4
        /// </summary>
        public int QueueCount { get; set; }

        /// <summary>
        /// 消息队列的容量
        /// 默认为1000
        /// </summary>
        public int QueueCapacity { get; set; }

        ///// <summary>
        ///// 是否启用命令处理器
        ///// </summary>
        //public bool EnableCommandProcessor { get; set; }
        ///// <summary>
        ///// 是否启用同步处理器
        ///// </summary>
        //public bool EnableSynchronousProcessor { get; set; }
        ///// <summary>
        ///// 是否启用事件处理器
        ///// </summary>
        //public bool EnableEventProcessor { get; set; }
    }
}
