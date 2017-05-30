using System.Runtime.Serialization;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// <see cref="IQuery"/> 的抽象类
    /// </summary>
    [DataContract]
    public abstract class QueryParameter : IQuery, IMessage, IUniquelyIdentifiable
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public QueryParameter()
        {
            this.Id = UniqueId.GenerateNewStringId();
        }

        /// <summary>
        /// 查询标识
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; private set; }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}", this.GetType().FullName, this.Id);
        }
    }
}
