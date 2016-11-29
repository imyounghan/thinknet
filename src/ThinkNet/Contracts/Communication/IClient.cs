
namespace ThinkNet.Contracts.Communication
{
    public interface IClient
    {
        TService CreateService<TService>();
    }
}
