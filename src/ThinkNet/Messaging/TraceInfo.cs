

namespace ThinkNet.Messaging
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// 表示一个
    /// </summary>
    [Serializable]
    [DataContract]
    public struct TraceInfo : ISerializable
    {
        public static readonly TraceInfo Empty = new TraceInfo();


        private string processId;

        private string replyAddress;

        public TraceInfo(string processId, string replyAddress)
        {
            this.processId = processId;
            this.replyAddress = replyAddress;
        }

        [DataMember(Name = "replyAddress")]
        public string ReplyAddress
        {
            get
            {
                return this.replyAddress;
            }
        }

        /// <summary>
        /// 源命令ID
        /// </summary>
        [DataMember(Name = "processId")]
        public string ProcessId
        {
            get
            {
                return this.processId;
            }
        }


        #region ISerializable 成员

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ProcessId", this.processId);
            info.AddValue("ReplyAddress", this.replyAddress);
        }

        #endregion
    }
}
