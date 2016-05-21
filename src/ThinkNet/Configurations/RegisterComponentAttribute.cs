using System;


namespace ThinkNet.Configurations
{
    /// <summary>
    /// 表示该特性的类会被优先注册到容器中。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RegisterComponentAttribute : ComponentAttribute
    {
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterComponentAttribute(string registerName = null)
        {
            base.RegisterName = registerName;
        }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterComponentAttribute(Type registerType, string registerName = null)
            : this(registerName)
        {
            registerType.NotNull("registerType");

            base.RegisterType = registerType;
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
        //    base.ImplementType = sourceType;
        //    base.Register(container);
        //} 

        public override void Register(Type sourceType, Action<ComponentAttribute> action)
        {
            base.ImplementType = sourceType;
            base.Register(sourceType, action);
        }
    }
}
