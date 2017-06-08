

namespace ThinkNet.Communication
{
    public class Response : IResponse
    {
        #region IResponse 成员

        public int Status { get; set; }

        public string ErrorMessage { get; set; }

        public string ErrorCode { get; set; }

        public string Result { get; set; }

        public string ResultType { get; set; }

        #endregion
    }
}
