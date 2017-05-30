

namespace ThinkNet.Infrastructure
{
    public abstract class Processor : DisposableObject, IProcessor
    {
        /// <summary>
        /// 用于锁的对象
        /// </summary>
        private readonly object lockObject;


        private bool started;

        protected Processor()
        {
            this.lockObject = new object();
        }

        /// <summary>
        /// 释放相关资源
        /// </summary>
        /// <param name="disposing">true表示释放资源</param>
        protected override void Dispose(bool disposing)
        { }

        #region IProcessor 成员
        /// <summary>
        /// 启动程序
        /// </summary>
        protected abstract void Start();

        /// <summary>
        /// 停止程序
        /// </summary>
        protected abstract void Stop();

        #endregion

        #region IProcessor 成员

        void IProcessor.Start()
        {
            lock(this.lockObject) {
                if(!this.started) {
                    this.Start();
                    this.started = true;
                }
            }
        }

        void IProcessor.Stop()
        {
            lock(this.lockObject) {
                if(this.started) {
                    this.Stop();
                    this.started = false;
                }
            }
        }

        #endregion
    }
}
