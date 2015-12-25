using System;

namespace ThinkNet.Kernel
{
    [Serializable]
    public class EventHandlerNotFoundException : EventSourcedException
    {
        private readonly string eventSourcedType;
        private readonly string eventType;

        public EventHandlerNotFoundException(Type eventSourcedType, Type eventType)
        {
            this.eventSourcedType = eventSourcedType.FullName;
            this.eventType = eventType.FullName;
        }
        public string EventSourcedType
        {
            get { return this.eventSourcedType; }
        }

        private string EventType 
        {
            get { return this.eventType; } 
        }

        public override string Message
        {
            get
            {
                return string.Format("Event handler not found on {0} for {1}.", EventSourcedType, EventType);
            }
        }
    }
}
