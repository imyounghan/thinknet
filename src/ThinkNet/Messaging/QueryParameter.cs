using System.Runtime.Serialization;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// <see cref="IQuery"/> 的抽象类
    /// </summary>
    [DataContract]
    public abstract class QueryParameter : IQuery, IMessage
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public QueryParameter()
        {
            this.Id = UniqueId.GenerateNewStringId();
        }

        string IMessage.GetKey()
        {
            return null;
        }

        [DataMember(Name = "id")]
        internal string Id;

        [IgnoreDataMember]
        string IUniquelyIdentifiable.Id
        {
            get { return this.Id; }
        }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}", this.GetType().FullName, this.Id);
        }
    }
}
