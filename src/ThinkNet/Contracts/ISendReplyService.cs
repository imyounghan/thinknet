
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISendReplyService
    {
        void SendReply(object replyData, string replyAddress);
    }
}
