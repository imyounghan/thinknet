

namespace ThinkNet.Messaging
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// 表示一个
    /// </summary>
    public struct TraceInfo : ISerializable
    {
        public static readonly TraceInfo Empty = new TraceInfo();


        private string traceId;

        private string traceAddress;

        public TraceInfo(string traceId, string traceAddress)
        {
            this.traceId = traceId;
            this.traceAddress = traceAddress;
        }
        
        public string Address
        {
            get
            {
                return this.traceAddress;
            }
        }

        /// <summary>
        /// 跟踪ID
        /// </summary>
        public string Id
        {
            get
            {
                return this.traceId;
            }
        }

        public override bool Equals(object obj)
        {
            if(!(obj is TraceInfo)) {
                return false;
            }

            TraceInfo other = (TraceInfo)obj;

            return this.traceId == other.traceId;
        }

        public override int GetHashCode()
        {
            return this.traceId.GetHashCode();
        }


        #region ISerializable 成员

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("TraceId", this.traceId);
            info.AddValue("TraceAddress", this.traceAddress);
        }

        #endregion
    }
}
