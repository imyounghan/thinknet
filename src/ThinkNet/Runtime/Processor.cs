using System;
using System.Collections.Generic;
using ThinkNet.Common.Interception;
using ThinkNet.Contracts;
using ThinkNet.Domain;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime.Dispatching;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime
{
    /// <summary>
    /// 框架内处理消息的核心进程
    /// </summary>
    public class Processor : DisposableObject, IProcessor
    {
        private readonly IEnvelopeReceiver _receiver;
        private readonly Dictionary<string, IDispatcher> _dispatcherDict;

        private readonly object lockObject;
        private bool started;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public Processor(IRepository repository,
            IEventSourcedRepository eventSourcedRepository,
            IEnvelopeReceiver receiver,
            ICommandResultNotification notification,
            IInterceptorProvider interceptorProvider,
            IHandlerRecordStore handlerStore,
            IMessageBus messageBus)
        {
            this._receiver = receiver;

            this._dispatcherDict = new Dictionary<string, IDispatcher>(StringComparer.CurrentCulture) {
                { StandardMetadata.CommandKind, new CommandDispatcher(repository, eventSourcedRepository, messageBus, handlerStore, interceptorProvider) },
                { StandardMetadata.EventKind, new EventDispatcher(handlerStore) },
                { StandardMetadata.MessageKind, new MessageDispatcher(handlerStore, messageBus, notification) }
            };
            this.lockObject = new object();
        }

        /// <summary>
        /// 添加一个调度器
        /// </summary>
        protected void AddDispatcher(string kind, IDispatcher dispatcher)
        {
            if (_dispatcherDict.ContainsKey(kind))
                return;

            _dispatcherDict[kind] = dispatcher;
        }

        /// <summary>
        /// 获取消息分类
        /// </summary>
        protected virtual string GetKind(object data)
        {
            if (data is IEvent)
                return StandardMetadata.EventKind;

            if (data is Messaging.ICommand)
                return StandardMetadata.CommandKind;

            if (data is IMessage)
                return StandardMetadata.MessageKind;

            return string.Empty;
        }


        private void OnEnvelopeReceived(object sender, Envelope envelope)
        {
            var kind = envelope.GetMetadata(StandardMetadata.Kind);
            if (string.IsNullOrEmpty(kind)) {
                kind = this.GetKind(envelope.Body);
            }
            if (string.IsNullOrEmpty(kind)) {
                //TODO...WriteLog
                return;
            }

            var message = envelope.Body as IMessage; 

            TimeSpan executionTime;
            _dispatcherDict[kind].Execute(message, out executionTime);
            envelope.ProcessTime = executionTime;

            envelope.Complete(sender);
        }

        /// <summary>
        /// 启动进程
        /// </summary>
        public void Start()
        {
            ThrowIfDisposed();
            lock (this.lockObject) {
                if (!this.started) {
                    _receiver.EnvelopeReceived += OnEnvelopeReceived;
                    _receiver.Start();
                    this.started = true;
                }
            }

            Console.WriteLine("Core Processor Started!");
        }

        /// <summary>
        /// 停止进程
        /// </summary>
        public void Stop()
        {
            lock (this.lockObject) {
                if (this.started) {
                    _receiver.EnvelopeReceived -= OnEnvelopeReceived;
                    _receiver.Stop();
                    this.started = false;
                }
            }

            Console.WriteLine("Core Processor Stopped!");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if (disposing) {
                this.Stop();
            }
        }
    }
}
