using System;

namespace ThinkNet.Common
{
    /// <summary>
    /// 后台循环执行一个特定的方法的工作器
    /// </summary>
    public class Worker<T> : Worker
    {
        private readonly T state;
        /// <summary>
        /// constructor.
        /// </summary>
        public Worker(Action<T> processor, T state, Action<T> successCallback, Action<T, Exception> exceptionCallback)
        {
            processor.NotNull("processor");
            state.NotNull("state");

            this.state = state;
            this.Processor = processor;            
            this.SuccessCallback = successCallback ?? EmptyMethod;
            this.ExceptionCallback = exceptionCallback ?? EmptyMethod;
        }

        /// <summary>
        /// constructor.
        /// </summary>
        public Worker(Action<T> processor, Func<T> factory, Action<T> successCallback, Action<T, Exception> exceptionCallback)
        {
            processor.NotNull("processor");
            factory.NotNull("factory");

            this.Factory = factory;
            this.Processor = processor;
            this.SuccessCallback = successCallback ?? EmptyMethod;
            this.ExceptionCallback = exceptionCallback ?? EmptyMethod;
        }


        private void EmptyMethod(T instance)
        { }

        private void EmptyMethod(T instance, Exception ex)
        { }

        /// <summary>
        /// 表示一个持续工作的方法
        /// </summary>
        protected override void Working()
        {
            var message = state == null ? Factory() : state;

            bool success = true;

            try {
                Processor.Invoke(message);
            }
            catch (Exception ex) {
                success = false;

                try {
                    ExceptionCallback(message, ex);
                }
                catch (Exception) {
                }

                //TODO.. Write LOG
            }

            if (success) {
                try {
                    SuccessCallback(message);
                }
                catch (Exception) {
                }
            }
        }


        /// <summary>
        /// 获取该 <typeparamref name="T"/> 实例的工厂方法。
        /// </summary>
        protected Func<T> Factory { get; private set; }

        /// <summary>
        /// 要执行的函数。
        /// </summary>
        new protected Action<T> Processor { get; private set; }

        /// <summary>
        /// 成功调用的函数
        /// </summary>
        new protected Action<T> SuccessCallback { get; private set; }

        /// <summary>
        /// 失败调用的函数
        /// </summary>
        new protected Action<T, Exception> ExceptionCallback { get; private set; }
    }
}
