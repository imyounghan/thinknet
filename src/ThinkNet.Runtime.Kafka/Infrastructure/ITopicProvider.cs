using System;
namespace ThinkNet.Infrastructure
{
    public interface ITopicProvider
    {
        string GetTopic(object payload);

        //string GetTopic(Type type);

        Type GetType(string topic);
    }
}
