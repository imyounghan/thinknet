using System;

namespace ThinkNet.Runtime.Executing
{
    public interface IExecutor
    {
        /// <summary>
        /// 执行消息结果。
        /// </summary>
        void Execute(object data, out TimeSpan processTime);
    }
}
