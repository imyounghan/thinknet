using System.Runtime.Serialization;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// <see cref="IQueryParameter"/> 的抽象类
    /// </summary>
    [DataContract]
    public abstract class QueryParameter : IQueryParameter
    { }
}
