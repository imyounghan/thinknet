
namespace ThinkNet.Messaging
{
    public interface IQueryBus
    {
        /// <summary>
        /// 发送一个查询命令。
        /// </summary>
        /// <param name="query">查询命令</param>
        /// <param name="traceInfo">跟踪信息</param>
        void Send(IQuery query, TraceInfo traceInfo);
    }
}
