

namespace ThinkNet.Communication
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Response : IResponse
    {
        #region IResponse 成员
        [DataMember(Name = "status")]
        public int Status { get; set; }

        [DataMember(Name = "errorMessage")]
        public string ErrorMessage { get; set; }

        [DataMember(Name = "errorCode")]
        public string ErrorCode { get; set; }

        [DataMember(Name = "result")]
        public string Result { get; set; }

        [DataMember(Name = "resultType")]
        public string ResultType { get; set; }

        #endregion
    }
}
