

namespace ThinkNet.Messaging.Handling
{
    using System.Collections;

    /// <summary>
    /// 表示处理程序上下文的类
    /// </summary>
    public class HandlerContext
    {
        /// <summary>
        /// 初始化 <see cref="HandlerContext"/> 类的新实例。
        /// </summary>
        /// <param name="handlerContext">消息处理程序上下文</param>
        protected HandlerContext(HandlerContext handlerContext)
        {
            handlerContext.NotNull("handlerContext");

            this.Message = handlerContext.Message;
            this.Handler = handlerContext.Handler;
            this.InvocationContext = handlerContext.InvocationContext;
        }

        /// <summary>
        /// 初始化 <see cref="HandlerContext"/> 类的新实例。
        /// </summary>
        /// <param name="message">一个消息</param>
        /// <param name="handler">消息处理程序</param>
        public HandlerContext(IMessage message, IHandler handler)
        {
            message.NotNull("message");
            handler.NotNull("handler");

            this.Message = message;
            this.Handler = handler;
            this.InvocationContext = new Hashtable();
        }


        /// <summary>
        /// 当前上下文数据
        /// </summary>
        public IDictionary InvocationContext { get; set; }

        /// <summary>
        /// 要处理的消息
        /// </summary>
        public IMessage Message { get; set; }

        /// <summary>
        /// 处理程序
        /// </summary>
        public IHandler Handler { get; set; }
    }
}
