using System.Configuration;

namespace ThinkNet
{
    /// <summary>
    /// 配置项
    /// </summary>
    public class ConfigurationSetting
    {
        /// <summary>
        /// 当前的一个实例
        /// </summary>
        public static readonly ConfigurationSetting Current = new ConfigurationSetting();


        private ConfigurationSetting()
        {
            this.HandleRetrytimes = ConfigurationManager.AppSettings["thinkcfg.retry_count"].ChangeIfError(5);
            this.HandleRetryInterval = ConfigurationManager.AppSettings["thinkcfg.retry_interval"].ChangeIfError(1000);
            this.QueueCount = ConfigurationManager.AppSettings["thinkcfg.queue_count"].ChangeIfError(4);            
            this.MaxRequests = ConfigurationManager.AppSettings["thinkcfg.server_maxrequests"].ChangeIfError(2000);
            this.OperationTimeout = ConfigurationManager.AppSettings["thinkcfg.server_timeout"].ChangeIfError(120);
            this.EnableCommandFilter = ConfigurationManager.AppSettings["thinkcfg.server_enablefilter"].ChangeIfError(false);
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
        /// 最大处理请求数
        /// 默认为2000
        /// </summary>
        public int MaxRequests { get; set; }

        /// <summary>
        /// 操作超时设置(单位:秒)
        /// 默认为120秒
        /// </summary>
        public int OperationTimeout { get; set; }

        /// <summary>
        /// 是否启用命令过滤器
        /// 默认为false，不启用
        /// </summary>
        public bool EnableCommandFilter { get; set; }
    }
}
