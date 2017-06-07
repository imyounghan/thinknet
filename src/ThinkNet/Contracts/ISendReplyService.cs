
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示发送回复的服务接口
    /// </summary>
    public interface ISendReplyService
    {
        void SendReply(object replyData, string replyAddress);
    }

    public class NonReplyService : ISendReplyService
    {

        #region ISendReplyService 成员

        public void SendReply(object replyData, string replyAddress)
        {
        }

        #endregion
    }
}
