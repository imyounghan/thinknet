using System;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// <see cref="IAggregateRoot"/> 的扩展类
    /// </summary>
    public static class AggregateRootExtensions
    {
        public static TRole ActAs<TRole>(IAggregateRoot aggregateRoot) where TRole : class
        {
            if (!typeof(TRole).IsInterface) {
                throw new AggregateRootException(string.Format("'{0}' is not an interface type.", typeof(TRole).FullName));
            }

            var actor = aggregateRoot as TRole;

            if (actor == null) {
                throw new AggregateRootException(string.Format("'{0}' cannot act as role '{1}'.", 
                    aggregateRoot.GetType().FullName, typeof(TRole).FullName));
            }

            return actor;
        }

        public static TAggregateRoot Clone<TAggregateRoot>(TAggregateRoot aggregateRoot) 
            where TAggregateRoot : class, IAggregateRoot
        {
            var cloneable = aggregateRoot as ICloneable;
            if (cloneable != null)
                return cloneable as TAggregateRoot;

            return aggregateRoot.Map<TAggregateRoot>();
        }
    }
}
