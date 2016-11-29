using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Contracts.Communication
{
    public interface IServer
    {
        void Startup();

        void Shutdown();
    }
}
