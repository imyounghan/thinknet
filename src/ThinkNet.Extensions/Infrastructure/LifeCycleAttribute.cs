using System;
using System.Reflection;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示实例的生命周期的特性(默认为Singleton)
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public class LifeCycleAttribute : Attribute
    {
        /// <summary>
        /// 生命周期
        /// </summary>
        [Serializable]
        public enum Lifecycle : byte
        {
            /// <summary>
            /// 每次都构造一个新实例
            /// </summary>
            Transient,
            /// <summary>
            /// 单例
            /// </summary>
            Singleton,
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LifeCycleAttribute()
            : this(Lifecycle.Singleton)
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public LifeCycleAttribute(Lifecycle lifecycle)
        {
            this.Lifetime = lifecycle;
        }

        /// <summary>
        /// 返回生命周期类型(默认为Singleton)
        /// </summary>
        public Lifecycle Lifetime { get; private set; }

        /// <summary>
        /// 获取生命周期
        /// </summary>
        public static Lifecycle GetLifecycle(Type type)
        {
            if (!type.IsDefined(typeof(LifeCycleAttribute), false))
                return Lifecycle.Singleton;

            return type.GetAttribute<LifeCycleAttribute>(false).Lifetime;
        }
    }
}
