using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkNet.Runtime
{
    public interface ITopicProvider
    {
        string GetTopic(object payload);
    }
}
