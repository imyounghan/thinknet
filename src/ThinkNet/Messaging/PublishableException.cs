
namespace ThinkNet.Messaging
{
    using System;
    using System.Runtime.Serialization;
    

    /// <summary>
    /// <see cref="IPublishableException"/> 的默认实现类
    /// </summary>
    public class PublishableException : Exception, IPublishableException
    {
        public PublishableException(string errorMessage, int errorCode)
            : this(errorMessage, errorCode, null)
        {
        }

        public PublishableException(string errorMessage, int errorCode, Exception innerException)
            : base(errorMessage, innerException)
        {
            this.Timestamp = DateTime.UtcNow;
            this.HResult = errorCode;
        }

        protected PublishableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.Timestamp = info.GetDateTime("timestamp");
        }


        public DateTime Timestamp { get; set; }
        

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("timestamp", this.Timestamp);
            //info.AddValue("sourceId", this.SourceId, typeof(string));
            //info.AddValue("correlationId", this.ProcessId, typeof(string));
            //info.AddValue("replyAddress", this.ReplyAddress, typeof(string));
        }

        /// <summary>
        /// 获取用于路由的关键字
        /// </summary>
        protected virtual string GetRoutingKey()
        {
            return null;
        }

        //#region IRoutingKeyProvider 成员

        //string IKeyProvider.GetKey()
        //{
        //    return this.GetRoutingKey();
        //}

        //#endregion



        string IPublishableException.ErrorCode
        {
            get
            {
                return this.HResult.ToString();
            }
        }

        
    }
}
