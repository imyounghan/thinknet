using ThinkLib.Composition;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示服务网关
    /// </summary>
    public static class ServiceGateway
    {
        static ServiceGateway()
        {
            Current = new DefaultServiceGateway();
            IsLocationProviderSet = false;
        }

        /// <summary>
        /// 表示当前的服务网关
        /// </summary>
        public static IServiceGateway Current { get; private set; }
        /// <summary>
        /// 是否为框架提供的
        /// </summary>
        public static bool IsLocationProviderSet { get; private set; }

        /// <summary>
        /// 设置网关的提供程序
        /// </summary>
        /// <param name="newProvider"></param>
        public static void SetGatewayProvider(ServiceGatewayProvider newProvider)
        {
            Current = newProvider.Invoke();
            IsLocationProviderSet = true;
        }

        class DefaultServiceGateway : IServiceGateway
        {

            #region IServiceGateway 成员

            public TService GetService<TService>()
            {
                return ObjectContainer.Instance.Resolve<TService>();
            }

            #endregion
        }
    }
}
