using System;

namespace ThinkNet.Database
{
    /// <summary>Represents an exception when tring to get a not existing aggregate root.
    /// </summary>
    [Serializable]
    public class AggregateRootException : ThinkNetException
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public AggregateRootException()
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public AggregateRootException(string message)
            : base(message)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public AggregateRootException(string message, Exception innerException)
            : base(message, innerException)
        { }    
    }
}
