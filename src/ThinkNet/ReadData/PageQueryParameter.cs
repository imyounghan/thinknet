using System.Runtime.Serialization;

namespace ThinkNet.ReadData
{
    [DataContract]
    public abstract class PageQueryParameter : QueryParameter
    {
        [DataMember]
        public int PageIndex { get; set; }

        [DataMember]
        public int PageSize { get; set; }
    }
}
