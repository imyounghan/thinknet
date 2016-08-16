
namespace ThinkNet.Infrastructure
{
    public interface ITopicProvider
    {
        string GetTopic(object payload);
    }
}
