
namespace ThinkNet.Common.Interception
{
    public delegate IMethodReturn InvokeInterceptorDelegate(IMethodInvocation input, GetNextInterceptorDelegate getNext);
}
