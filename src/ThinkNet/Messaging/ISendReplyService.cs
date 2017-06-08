
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示发送回复的服务接口
    /// </summary>
    public interface ISendReplyService
    {
        void SendReply(IReplyResult replyResult, string replyAddress);
    }
}
