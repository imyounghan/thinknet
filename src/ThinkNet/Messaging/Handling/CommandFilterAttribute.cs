using System;

namespace ThinkNet.Messaging.Handling
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public abstract class CommandFilterAttribute : Attribute, ICommandFilter
    {
        public int Order { get; set; }

        #region ICommandFilter 成员

        public virtual void OnCommandHandled(CommandHandledContext filterContext)
        { }

        public virtual void OnCommandHandling(CommandHandlingContext filterContext)
        { }

        #endregion
    }
}
