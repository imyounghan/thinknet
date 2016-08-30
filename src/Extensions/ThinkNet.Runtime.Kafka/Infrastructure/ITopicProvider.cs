using System;
namespace ThinkNet.Infrastructure
{
    public interface ITopicProvider
    {
        string GetTopic(object payload);

        Type GetType(string topic);
    }
}
