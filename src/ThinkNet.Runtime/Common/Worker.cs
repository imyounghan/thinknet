using System;
using System.Threading;
using System.Threading.Tasks;


namespace ThinkNet.Common
{
    /// <summary>
    /// 后台循环执行一个特定的方法的工作器
    /// </summary>
    public class Worker
    {
        /// <summary>Initialize a new Worker for the specified method to run.
        /// </summary>
        public Worker(Action processor, Action successCallback, Action<Exception> exceptionCallback)
        {
            processor.NotNull("processor");

            this.Processor = processor;
            this.SuccessCallback = successCallback ?? EmptyMethod;
            this.ExceptionCallback = exceptionCallback ?? EmptyMethod;
        }

        /// <summary>
        /// default constructor.
        /// </summary>
        protected Worker()
        { }

        private void EmptyMethod()
        { }

        private void EmptyMethod(Exception ex)
        { }

        /// <summary>
        /// 间隔时间(毫秒数)
        /// </summary>
        public int Interval { get; private set; }

        /// <summary>
        /// 要执行的函数。
        /// </summary>
        protected Action Processor { get; private set; }

        /// <summary>
        /// 成功调用的函数
        /// </summary>
        protected Action SuccessCallback { get; private set; }

        /// <summary>
        /// 失败调用的函数
        /// </summary>
        protected Action<Exception> ExceptionCallback { get; private set; }


        private void AlwaysRunning()
        {
            while (!cancellationSource.IsCancellationRequested) {
                this.Working();

                if (this.Interval > 0)
                    Thread.Sleep(Interval);
            }
        }

        private static readonly CancellationToken cancellationToken = new CancellationToken(true);
        /// <summary>
        /// 获取取消操作的通知
        /// </summary>
        public CancellationToken CancellationToken
        {
            get
            {
                return this.cancellationSource == null ? cancellationToken : this.cancellationSource.Token;
            }
        }

        /// <summary>
        /// 等待下一个任务的间隔时间
        /// </summary>
        /// <param name="interval">毫秒</param>
        public void SetInterval(int interval)
        {
            this.Interval = interval;
        }

        private CancellationTokenSource cancellationSource;
        /// <summary>
        /// Start the worker.
        /// </summary>
        public void Start()
        {
            if (this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();
                Task.Factory.StartNew(
                     this.AlwaysRunning,
                     this.cancellationSource.Token,
                     TaskCreationOptions.LongRunning,
                     TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Stop the worker.
        /// </summary>
        public void Stop()
        {
            if (this.cancellationSource != null) {
                using (this.cancellationSource) {
                    this.cancellationSource.Cancel();
                }

                this.cancellationSource = null;
            }
        }

        /// <summary>
        /// 表示一个持续工作的方法
        /// </summary>
        protected virtual void Working()
        {
            bool success = true;

            try {
                Processor.Invoke();
            }
            catch (Exception ex) {
                success = false;

                try {
                    ExceptionCallback(ex);
                }
                catch (Exception) {
                }
            }
            if (success) {
                try {
                    SuccessCallback();
                }
                catch (Exception) {
                }
            }
        }
    }
}
