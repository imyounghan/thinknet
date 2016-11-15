using System;

namespace ThinkNet.Messaging.Handling
{
    public class CommandHandledContext
    {
        public ICommand Command { get; set; }

        public Exception Exception { get; set; }
    }
}
