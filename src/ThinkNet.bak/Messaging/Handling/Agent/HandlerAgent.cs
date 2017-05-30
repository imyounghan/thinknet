using System;
using System.Linq;
using System.Threading;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// <see cref="IHandlerAgent"/> 的抽象实现类
    /// </summary>
    public abstract class HandlerAgent : DisposableObject, IHandlerAgent
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        public virtual void Handle(object[] args)
        {
            TryMultipleHandle(this, args);
        }

        /// <summary>
        /// 尝试处理消息
        /// </summary>
        protected abstract void TryHandle(object[] args);

        //protected T GetValue<T>(IEnumerable<object> args)
        //    where T : class
        //{
        //    return args.FirstOrDefault(p => p.GetType() == typeof(T)) as T;
        //}

        private static void TryMultipleHandle(HandlerAgent handler, object[] args)
        {
            //private static readonly int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;
            //private static readonly int retryInterval = ConfigurationSetting.Current.HandleRetryInterval;

            TryMultipleHandle(handler, args,
                ConfigurationSetting.Current.HandleRetrytimes,
                ConfigurationSetting.Current.HandleRetryInterval);
        }

        private static void TryMultipleHandle(HandlerAgent handler, object[] args, int retryTimes, int retryInterval)
        {
            int count = 0;
            while (count++ < retryTimes) {
                try {
                    handler.TryHandle(args);
                    break;
                }
                catch (ThinkNetException) {
                    throw;
                }
                catch (Exception ex) {
                    if (count == retryTimes) {
                        throw new ThinkNetException(ex.Message, ex);
                    }
                    if (LogManager.Default.IsWarnEnabled) {
                        var messageString = args.Last().ToString();
                        if(args.Length > 2) {
                            messageString = string.Join(";", args.Skip(1));
                        }                        
                        LogManager.Default.Warn(ex,
                            "An exception happened while handling ({0}) through handler on ({1}), Error will be ignored and retry again({2}).",
                             messageString, handler.GetInnerHandler().GetType().FullName, count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }

            if (LogManager.Default.IsDebugEnabled) {
                var messageString = args.Last().ToString();
                if(args.Length > 2) {
                    messageString = string.Join(";", args.Skip(1));
                }
                LogManager.Default.DebugFormat("Handle ({0}) on ({1}) successfully.", 
                    messageString, handler.GetInnerHandler().GetType().FullName);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            var lifecycle = LifeCycleAttribute.GetLifecycle(GetInnerHandler().GetType());
            if(lifecycle == Lifecycle.Transient && disposing) {
                using (GetInnerHandler() as IDisposable) {
                    // Dispose handler if it's disposable.
                }
            }
        }

        /// <summary>
        /// 获取目标处理器
        /// </summary>
        public abstract object GetInnerHandler();        

    }
}
