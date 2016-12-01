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
    public abstract class QueryParameter : IQueryParameter, IMessage, IUniquelyIdentifiable
    {
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
    }
}
