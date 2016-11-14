using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ThinkNet.Common.Composition;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Runtime.Executing
{
    public abstract class Executor<TData> : IExecutor
        where TData : class
    {
        private readonly int _retryTimes;
        private readonly int _retryInterval;
        private int _executionCount;

        public Executor()
            : this(ConfigurationSetting.Current.HandleRetrytimes, ConfigurationSetting.Current.HandleRetryInterval)
        { }

        protected Executor(int retryTimes, int retryInterval)
        {
            this._retryInterval = retryInterval;
            this._retryTimes = retryTimes;
        }


        protected virtual IEnumerable<IProxyHandler> GetHandlers(Type type)
        {
            var contractType = typeof(IMessageHandler<>).MakeGenericType(type);
            return ObjectContainer.Instance.ResolveAll(contractType)
                .Cast<IHandler>()
                .Select(handler => new MessageHandlerWrapper(handler, contractType))
                .Cast<IProxyHandler>();
        }

        protected TimeSpan TryMultipleInvokeHandler(object data, IProxyHandler handler, Stopwatch stopwatch)
        {
            _executionCount++;

            try {
                stopwatch.Restart();
                handler.Handle(data);
                stopwatch.Stop();

                return stopwatch.Elapsed;
            }
            catch (ThinkNetException) {
                throw;
            }
            catch (Exception ex) {
                if (_executionCount == _retryTimes) {
                    throw new ThinkNetException(ex.Message, ex);
                }

                if (LogManager.Default.IsWarnEnabled) {
                    LogManager.Default.Warn(ex,
                        "An exception happened while processing '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                         data, handler.TargetType.FullName, _executionCount);
                }
                Thread.Sleep(_retryInterval);

                return TryMultipleInvokeHandler(data, handler, stopwatch);
            }
        }



        //public virtual void Execute(object data, out TimeSpan processTime)
        //{
        //    processTime = TimeSpan.Zero;

        //    var stopwatch = Stopwatch.StartNew();
        //    var handlers = this.GetHandlers(data.GetType());
        //    foreach (var handler in handlers) {
        //        try {
        //            processTime += TryMultipleInvokeHandler(data, handler, stopwatch);
        //        }
        //        catch (Exception ex) {
        //            this.OnException(ex);
        //        }
        //    }
        //}

        protected virtual void OnExecuting(TData data, Type handlerType)
        { }

        protected virtual void OnExecuted(TData data, Type handlerType, Exception ex)
        { }

        protected virtual void OnException(Exception ex)
        { }

        public void Execute(object data, out TimeSpan processTime)
        {
            processTime = TimeSpan.Zero;
            var message = data as TData;
            if (message == null) {
                //TODO....WriteLog
                return;// false;
            }

            var stopwatch = Stopwatch.StartNew();
            var handlers = this.GetHandlers(data.GetType());
            foreach (var handler in handlers) {
                try {
                    processTime += InvokeHandler(message, handler, stopwatch);
                }
                catch (Exception ex) {
                    this.OnException(ex);
                }
            }
        }

        private TimeSpan InvokeHandler(TData data, IProxyHandler handler, Stopwatch stopwatch)
        {
            bool wasError = false;
            try {
                this.OnExecuting(data, handler.TargetType);
                return TryMultipleInvokeHandler(data, handler, stopwatch);
            }
            catch (Exception ex) {
                this.OnExecuted(data, handler.TargetType, ex);
                throw ex;
            }
            finally {
                if (!wasError) {
                    this.OnExecuted(data, handler.TargetType, null);
                }
            }
        }
    }
}
