using System;

namespace ThinkNet
{
    /// <summary>
    /// 表示框架执行过程发生的错误
    /// </summary>
    [Serializable]
    public class ThinkNetException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ThinkNetException()
        {
            this.MessageCode = "-1";
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public ThinkNetException(string message)
            : base(message)
        {
            this.MessageCode = "-1";
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public ThinkNetException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.MessageCode = "-1";
        }

        /// <summary>
        /// 错误编码
        /// </summary>
        public string MessageCode { get; set; }
    }
}
