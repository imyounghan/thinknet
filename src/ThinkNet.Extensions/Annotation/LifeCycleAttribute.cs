using System;
using System.Reflection;

namespace ThinkNet.Annotation
{
    /// <summary>
    /// 表示实例的生命周期的特性(默认为Singleton)
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public class LifeCycleAttribute : Attribute
    {
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
            this.Lifecycle = lifecycle;
        }

        /// <summary>
        /// 返回生命周期类型(默认为Singleton)
        /// </summary>
        public Lifecycle Lifecycle { get; private set; }

        /// <summary>
        /// 获取生命周期
        /// </summary>
        public static Lifecycle GetLifecycle(Type type)
        {
            if (!type.IsDefined(typeof(LifeCycleAttribute), false))
                return Lifecycle.Singleton;

            return type.GetAttribute<LifeCycleAttribute>(false).Lifecycle;
        }
    }
}
