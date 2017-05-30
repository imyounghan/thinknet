
namespace ThinkNet.Runtime.Routing
{
    /// <summary>
    /// Exposes the property names of standard metadata added to all messages going through the bus.
    /// </summary>
    public static class StandardMetadata
    {
        /// <summary>
        /// An event message.
        /// </summary>
        public const string EventKind = "Event";

        /// <summary>
        /// A command message.
        /// </summary>
        public const string CommandKind = "Command";

        /// <summary>
        /// A query message.
        /// </summary>
        public const string QueryKind = "Query";

        /// <summary>
        /// A message.
        /// </summary>
        public const string MessageKind = "Message";

        /// <summary>
        /// Kind of message, either <see cref="EventKind"/> or <see cref="CommandKind"/>.
        /// </summary>
        public const string Kind = "Kind";
        
        /// <summary>
        /// The simple assembly name of the message payload (i.e. event or command).
        /// </summary>
        public const string AssemblyName = "AssemblyName";

        /// <summary>
        /// The namespace of the message payload (i.e. event or command).
        /// </summary>
        public const string Namespace = "Namespace";

        /// <summary>
        /// The full type name of the message payload (i.e. event or command).
        /// </summary>
        public const string TypeFullName = "TypeFullName";

        /// <summary>
        /// The simple type name (without the namespace) of the message payload (i.e. event or command).
        /// </summary>
        public const string TypeName = "TypeName";

        /// <summary>
        /// Identifier of the object that originated the event, if any.
        /// </summary>
        public const string SourceId = "SourceId";

        /// <summary>
        /// The name of the entity that originated this message.
        /// </summary>
        public const string SourceType = "SourceType";

        ///// <summary>
        ///// Identifier of the object
        ///// </summary>
        //public const string IdentifierId = "IdentifierId";

        //public const string RoutingKey = "RoutingKey";
    }
}
