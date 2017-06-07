

namespace ThinkNet.Messaging
{
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;

    using ThinkNet.Infrastructure;

    public class EventProducer : MessageProducer<EventCollection>, IEventBus
    {

        public EventProducer(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
        }


        #region IEventBus 成员

        public void Publish(SourceKey sourceInfo, EventCollection eventCollection, Envelope<Command> command)
        {
            var envelope = new Envelope<EventCollection>(
                eventCollection,
                MD5(string.Format("{0}@{1}", sourceInfo.Id, command.MessageId)),
                sourceInfo.Id);
            envelope.Items["TraceInfo"] = command.Items["TraceInfo"];
            envelope.Items["SourceKey"] = sourceInfo;

            this.Append(envelope);
        }


        static string MD5(string source)
        {
            StringBuilder sb = new StringBuilder(32);

            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] t = md5.ComputeHash(Encoding.UTF8.GetBytes(source));
                for (int i = 0; i < t.Length; i++)
                {
                    sb.Append(t[i].ToString("x").PadLeft(2, '0'));
                }
            }

            return sb.ToString();
        }
        #endregion
    }
}
