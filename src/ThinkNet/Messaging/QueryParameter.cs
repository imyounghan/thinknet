using System;
using System.Runtime.Serialization;
using ThinkLib.Utilities;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// <see cref="IQueryParameter"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class QueryParameter : IQueryParameter, IMessage, IUniquelyIdentifiable
    {
        public QueryParameter()
        {
            this.Id = UniqueId.GenerateNewStringId();
        }

        

        [DataMember(Name = "id")]
        internal string Id { get; set; }

        string IMessage.GetKey()
        {
            return null;
        }

        [IgnoreDataMember]
        string IUniquelyIdentifiable.Id
        {
            get { return this.Id; }
        }
    }
}
