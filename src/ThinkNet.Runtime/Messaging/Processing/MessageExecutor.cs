﻿using System;
using System.Diagnostics;
using System.Threading;
using ThinkNet.Configurations;

namespace ThinkNet.Messaging.Processing
{
    public abstract class MessageExecutor<TMessage> : IExecutor
        where TMessage : class, IMessage
    {
        private readonly int _retryTimes;

        protected MessageExecutor()
            : this(ConfigurationSetting.Current.HandleRetrytimes)
        { }
        protected MessageExecutor(int retryTimes)
        {
            this._retryTimes = retryTimes;
        }

        protected abstract void Execute(TMessage message);

        //protected virtual void Notify(TMessage message, Exception exception)
        //{
        //    if(exception == null) {
        //        if(LogManager.Default.IsDebugEnabled) {
        //            LogManager.Default.DebugFormat("Handle {0} success.", message);
        //        }
        //    }
        //    else {
        //        if(LogManager.Default.IsErrorEnabled) {
        //            LogManager.Default.Error(exception, "Exception raised when handling {0}.", message);
        //        }
        //    }
        //}

        protected virtual void OnExecuted(TMessage message)
        {
            if (LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Handle {0} success.", message);
            }
        }

        protected virtual void OnException(TMessage message, Exception ex)
        {
            if (LogManager.Default.IsErrorEnabled) {
                LogManager.Default.Error(ex, "Exception raised when handling {0}.", message);
            }
        }

        private bool Execute(TMessage message, ref TimeSpan processTime)
        {
            int count = 0;
            Exception exception = null;
            while (count++ < _retryTimes) {
                try {
                    var sw = Stopwatch.StartNew();
                    this.Execute(message);
                    sw.Stop();
                    processTime = sw.Elapsed;
                    break;
                }
                catch (ThinkNetException ex) {
                    exception = ex;
                    break;
                }
                catch (Exception ex) {
                    if (count == _retryTimes) {
                        exception = ex;
                        break;
                    }

                    if (LogManager.Default.IsWarnEnabled) {
                        LogManager.Default.Warn(ex,
                            "An exception happened while processing {0} through handler, Error will be ignored and retry again({1}).",
                             message, count);
                    }
                    Thread.Sleep(ConfigurationSetting.Current.HandleRetryInterval);
                }
            }

            if (exception == null) {
                this.OnExecuted(message);
                return true;
            }
            else {
                this.OnException(message, exception);
                return false;
            }
        }

        bool IExecutor.Execute(object data, out TimeSpan processTime)
        {
            processTime = TimeSpan.Zero;
            var message = data as TMessage;
            if (message == null) {
                //TODO....WriteLog
                return false;
            }

            return this.Execute(message, ref processTime);
        }
    }
}
