using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Infrastructure;


namespace ThinkNet.Messaging
{
    public abstract class AbstractBus : IInitializer
    {
        /// <summary>
        /// 检索匹配的类型
        /// </summary>
        protected abstract bool SearchMatchType(Type type);

        /// <summary>
        /// 初始化操作
        /// </summary>
        protected virtual void Initialize(IEnumerable<Type> types)
        { }

        void IInitializer.Initialize(IEnumerable<Type> types)
        {
            //var types = assemblies.SelectMany(assembly => assembly.GetTypes());
            types.Where(SearchMatchType).ForEach(type => {
                if (!type.IsSerializable) {
                    string message = string.Format("{0} should be marked as serializable.", type.FullName);
                    throw new ThinkNetException(message);
                }
            });

            this.Initialize(types);
        }
    }
}
