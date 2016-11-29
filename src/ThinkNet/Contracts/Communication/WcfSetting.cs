
namespace ThinkNet.Contracts.Communication
{
    public class WcfSetting
    {
        public enum BindingMode
        {
            Http,
            Tcp
        }

        private WcfSetting()
        {
            Scheme = BindingMode.Tcp;
            IpAddress = "127.0.0.1";
            Port = 8081;
        }

        public static BindingMode Scheme { get; set; }

        public static string IpAddress { get; set; }

        public static int Port { get; set; }
    }
}
