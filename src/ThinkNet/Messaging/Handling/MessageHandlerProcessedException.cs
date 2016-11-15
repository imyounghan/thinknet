
namespace ThinkNet.Messaging.Handling
{
    public class MessageHandlerProcessedException : ThinkNetException
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MessageHandlerProcessedException()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public MessageHandlerProcessedException(string message)
            : base(message)
        { }
    }
}
