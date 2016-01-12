using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;
using ThinkNet.Messaging.Queuing;


namespace ThinkNet.Messaging.Handling
{
    public class MessageProcessor : DisposableObject, IInitializer, IProcessor
    {
        private readonly IMessageReceiver receiver;
        private readonly IMessageExecutor executor;
        private readonly IEventPublishedVersionStore eventPublishedVersionStore;
        private readonly IMessageBroker broker;
        private readonly ICommandResultManager commandResultManager;
        
        private readonly object lockObject = new object();
        private bool started = false;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageProcessor(IMessageReceiver receiver,
            IMessageExecutor executor,
            IEventPublishedVersionStore eventPublishedVersionStore,
            IMessageBroker broker,
            ICommandResultManager commandResultManager)
        {
            this.receiver = receiver;
            this.executor = executor;
            this.eventPublishedVersionStore = eventPublishedVersionStore;
            this.broker = broker;
            this.commandResultManager = commandResultManager;
        }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public virtual void Start()
        {
            ThrowIfDisposed();
            lock (this.lockObject) {
                if (!this.started) {
                    this.receiver.MessageReceived += OnMessageReceived;
                    this.receiver.Start();
                    this.started = true;
                }
            }
        }

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public virtual void Stop()
        {
            lock (this.lockObject) {
                if (this.started) {
                    this.receiver.Stop();
                    this.receiver.MessageReceived -= OnMessageReceived;
                    this.started = false;
                }
            }
        }
               
        
        /// <summary>
        /// Disposes the resources used by the processor.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if (disposing) {
                this.Stop();

                using (this.receiver as IDisposable) {
                    // Dispose receiver if it's disposable.
                }
            }
        }

        protected virtual void ProcessMessage(Message message)
        {
            var msg = message.Body as IMessage;
            if (msg == null)
                return;

            int count = 0;
            int retryTimes = 1;

            while (count++ < retryTimes) {
                try {
                    executor.Execute(msg);
                    break;
                }
                catch (ThinkNetException) {
                    throw;
                }
                catch (Exception) {
                    if (count == retryTimes)
                        throw;
                    else
                        Thread.Sleep(1000);
                }
            }
        }

        private bool OnProcessing(Message message)
        {
            var stream = message.Body as VersionedEventStream;
            if (stream != null) {
                var sourceKey = new SourceKey(
                    message.MetadataInfo[StandardMetadata.SourceId],
                    message.MetadataInfo[StandardMetadata.Namespace],
                    message.MetadataInfo[StandardMetadata.TypeName],
                    message.MetadataInfo[StandardMetadata.AssemblyName]
                    );

                var version = eventPublishedVersionStore.GetPublishedVersion(sourceKey);

                if (version + 1 != stream.StartVersion) { //如果当前的消息版本不是要处理的情况
                    if (stream.StartVersion > version + 1) //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                        broker.TryAdd(message);
                    return false;
                }
            }

            return true;
        }

        private void OnProcessed(Message message)
        {
            var command = message.Body as ICommand;
            if (command != null) {
                commandResultManager.NotifyCommandExecuted(command.Id, CommandStatus.Success, null);
                return;
            }

            var @event = message.Body as EventStream;
            if (@event != null) {
                commandResultManager.NotifyCommandCompleted(@event.CommandId, 
                    @event.Events.IsEmpty() ? CommandStatus.NothingChanged : CommandStatus.Success);
                return;
            }
        }

        private void OnException(Message message, Exception ex)
        {
            var command = message.Body as ICommand;
            if (command != null) {
                commandResultManager.NotifyCommandExecuted(command.Id, CommandStatus.Failed, ex);
                return;
            }

            var @event = message.Body as EventStream;
            if (@event != null) {
                commandResultManager.NotifyCommandCompleted(@event.CommandId, CommandStatus.Failed, ex);
                return;
            }
        }


        private void OnMessageReceived(object sender, EventArgs<Message> args)
        {
            try {
                if (OnProcessing(args.Data)) { //如果是有序的消息
                    ProcessMessage(args.Data);
                    OnProcessed(args.Data);
                }
            }
            catch (Exception ex) {
                OnException(args.Data, ex);
                // NOTE: we catch ANY exceptions as this is for local 
                // development/debugging. The Windows Azure implementation 
                // supports retries and dead-lettering, which would 
                // be totally overkill for this alternative debug-only implementation.
                //Trace.TraceError("An exception happened while processing message through handler/s:\r\n{0}", e);
                //Trace.TraceWarning("Error will be ignored and message receiving will continue.");
            }
        }

        

        #region IInitializer 成员
        private static bool IsHandlerType(Type type)
        {
            return type.IsClass && !type.IsAbstract && type.IsAssignableFrom(typeof(IHandler));
        }

        private static bool IsRegisterType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();
            return genericType == typeof(IMessageHandler<>) ||
                genericType == typeof(ICommandHandler<>) ||
                genericType == typeof(IEventHandler<>);
        }

        private void RegisterType(Type type)
        {
            var interfaceTypes = type.GetInterfaces().Where(IsRegisterType);

            var lifetime = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);
            foreach (var interfaceType in interfaceTypes) {
                Configuration.Current.RegisterType(interfaceType, type, lifetime, type.FullName);
            }
        }

        public void Initialize(IEnumerable<Type> types)
        {
            AggregateRootInnerHandlerUtil.Initialize(types);

            types.Where(IsHandlerType).ForEach(RegisterType);
        }

        #endregion
    }
}
