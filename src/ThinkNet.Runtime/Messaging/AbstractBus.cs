using System;
using System.Collections.Generic;
using System.Linq;


namespace ThinkNet.Messaging
{
    public abstract class AbstractBus : DisposableObject, IInitializer
    {
        /// <summary>
        /// 检索匹配的类型
        /// </summary>
        protected abstract bool MatchType(Type type);

        /// <summary>
        /// 初始化操作
        /// </summary>
        protected virtual void Initialize(IEnumerable<Type> types)
        { }

        protected override void Dispose(bool disposing)
        { }

        void IInitializer.Initialize(IEnumerable<Type> types)
        {
            //var types = assemblies.SelectMany(assembly => assembly.GetTypes());
            types.Where(MatchType).ForEach(type => {
                if (!type.IsSerializable) {
                    string message = string.Format("{0} should be marked as serializable.", type.FullName);
                    throw new ApplicationException(message);
                }
            });

            this.Initialize(types);
        }
    }
}
