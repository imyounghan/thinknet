
namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 模型验证结果
    /// </summary>
    public class ModelValidationResult
    {
        private string _memberName;
        private string _message;

        /// <summary>
        /// 成员名称
        /// </summary>
        public string MemberName
        {
            get
            {
                return _memberName ?? string.Empty;
            }
            set
            {
                _memberName = value;
            }
        }
        /// <summary>
        /// 验证消息
        /// </summary>
        public string Message
        {
            get
            {
                return _message ?? string.Empty;
            }
            set
            {
                _message = value;
            }
        }
    }
}
