
namespace ThinkNet.Common.Interception
{
    public interface IInterceptor
    {
        IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptorDelegate getNext);
    }
}
