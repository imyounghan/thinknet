using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Messaging.Handling
{
    public class CommandHandlingContext
    {
        public ICommand CommandInfo { get; }

        public IHandler CommandHandler { get; }

        //public ICommandContext CommandContext { get; }
    }
}
