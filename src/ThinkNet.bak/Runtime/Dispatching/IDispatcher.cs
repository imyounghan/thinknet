using System;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 表示继承该接口的是一个调试程序
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// 执行结果。
        /// </summary>
        void Execute(object arg, out TimeSpan time);
    }
}
