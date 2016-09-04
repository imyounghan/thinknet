﻿using System;
using ThinkNet.EventSourcing;
using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    public class DefaultTopicProvider : ITopicProvider
    {
        //public string GetTopic(Type type)
        //{
        //    if(type == typeof(EventStream) || type == typeof(VersionedEvent))
        //        return "EventStreams";

        //    if(type == typeof(RepliedCommand))
        //        return "CommandResults";

        //    if(TypeHelper.IsCommand(type))
        //        return "Commands";

        //    if(TypeHelper.IsEvent(type))
        //        return "Events";

        //    throw new ThinkNetException(string.Format("Unknown topic from the type of '{0}'.", type.FullName));
        //}

        public string GetTopic(object payload)
        {
            if (payload is VersionedEvent) {
                return "EventStreams";
            }

            if (payload is RepliedCommand) {
                return "CommandResults";
            }

            if (payload is ICommand) {
                return "Commands";
            }

            if (payload is IEvent) {
                return "Events";
            }            

            throw new ThinkNetException(string.Format("Unknown topic from the type of '{0}'.", payload.GetType().FullName));
        }

        public Type GetType(string topic)
        {
            switch(topic) {
                case "EventStreams":
                    return typeof(EventStream);
                case "CommandResults":
                    return typeof(RepliedCommand);
                case "Commands":
                case "Events":
                    return typeof(GeneralData);
                default:
                    throw new ThinkNetException(string.Format("Unknown topic of '{0}'.", topic));
            }
        }        
    }
}
