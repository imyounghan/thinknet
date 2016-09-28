using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Messaging.Handling
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
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
