using KafkaNet;

namespace ThinkNet.Infrastructure
{
    public class KafkaLog : IKafkaLog
    {
        public static readonly IKafkaLog Instance = new KafkaLog();


        private readonly LogManager.ILogger logger;
        private KafkaLog()
        {
            this.logger = LogManager.GetLogger("Kafka");
        }


        #region IKafkaLog 成员

        public void DebugFormat(string format, params object[] args)
        {
            if (logger.IsDebugEnabled)
                logger.DebugFormat(format, args);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            if (logger.IsErrorEnabled)
                logger.ErrorFormat(format, args);
        }

        public void FatalFormat(string format, params object[] args)
        {
            if (logger.IsFatalEnabled)
                logger.FatalFormat(format, args);
        }

        public void InfoFormat(string format, params object[] args)
        {
            if (logger.IsInfoEnabled)
                logger.InfoFormat(format, args);
        }

        public void WarnFormat(string format, params object[] args)
        {
            if (logger.IsWarnEnabled)
                logger.WarnFormat(format, args);
        }

        #endregion
    }
}
