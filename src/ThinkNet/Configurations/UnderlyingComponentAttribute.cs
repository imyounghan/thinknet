using System;

namespace ThinkNet.Configurations
{
    /// <summary>
    /// 标记此特性的类型是基础组件。
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public class UnderlyingComponentAttribute : ComponentAttribute
    {
        /// <summary>
        /// default constructor.
        /// </summary>
        public UnderlyingComponentAttribute(string registerName = null)
        {
            base.RegisterName = registerName;
        }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public UnderlyingComponentAttribute(Type implementType, string registerName = null)
            : this(registerName)
        {
            implementType.NotNull("implementType");

            base.ImplementType = implementType;
        }

        /// <summary>
        /// 类型注册
        /// </summary>
        public override void Register(Type sourceType, Action<ComponentAttribute> action)
        {
            base.RegisterType = sourceType;
            base.Register(sourceType, action);
        }
        /////// <summary>
        /////// 注册顺序
        /////// </summary>
        ////public override int Order
        ////{
        ////    get { return (int)byte.MinValue; }
        ////}
        ///// <summary>
        ///// 类型注册
        ///// </summary>
        //public override void Register(Bootstrapper container, Type sourceType)
        //{
        //    base.RegisterType = sourceType;
        //    base.Register(container);
        //} 
    }
}
