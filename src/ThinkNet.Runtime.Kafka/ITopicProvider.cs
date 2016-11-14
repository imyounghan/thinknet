using System;
namespace ThinkNet.Runtime.Kafka
{
    public interface ITopicProvider
    {
        string GetTopic(object payload);

        //string GetTopic(Type type);

        Type GetType(string topic);
    }
}
