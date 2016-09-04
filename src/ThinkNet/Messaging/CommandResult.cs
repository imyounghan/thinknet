using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;


namespace ThinkNet.Messaging
{
    /// <summary>
    /// 命令处理结果
    /// </summary>
    [DataContract]
    [Serializable]
    public class CommandResult
    {
        /// <summary>
        /// 命令处理状态。
        /// </summary>
        [DataMember]
        public CommandStatus Status { get; private set; }
        /// <summary>
        /// Represents the unique identifier of the command.
        /// </summary>
        [DataMember]
        public string CommandId { get; private set; }
        /// <summary>
        /// 异常类型名称
        /// </summary>
        [DataMember]
        public string ExceptionTypeName { get; private set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; private set; }
        /// <summary>
        /// 错误编码
        /// </summary>
        [DataMember]
        public string ErrorCode { get; private set; }
        /// <summary>
        /// 设置或获取一个提供用户定义的其他异常信息的键/值对的集合。
        /// </summary>
        [DataMember]
        public IDictionary ErrorData { get; private set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected CommandResult()
        {
            this.Status = CommandStatus.Success;
        }

        /// <summary>Parameterized constructor.
        /// </summary>
        public CommandResult(CommandStatus status, string commandId)
            : this(status, commandId, string.Empty, string.Empty, new Dictionary<string, string>())
        { }
        /// <summary>Parameterized constructor.
        /// </summary>
        public CommandResult(CommandStatus status, string commandId, string exceptionTypeName, string errorMessage)
            : this(status, commandId, exceptionTypeName, errorMessage, new Dictionary<string, string>())
        { }
        /// <summary>Parameterized constructor.
        /// </summary>
        public CommandResult(CommandStatus status, string commandId, string exceptionTypeName, string errorMessage, IDictionary errorData)
        {
            this.Status = status;
            this.CommandId = commandId;
            this.ExceptionTypeName = exceptionTypeName;
            this.ErrorMessage = errorMessage;
            this.ErrorData = errorData;
        }

        [NonSerialized]
        private readonly Exception _exception = null;
        /// <summary>Parameterized constructor.
        /// </summary>
        public CommandResult(string commandId, Exception exception)
            : this(exception == null ? CommandStatus.Success : CommandStatus.Failed, commandId, exception)
        { }

        /// <summary>Parameterized constructor.
        /// </summary>
        public CommandResult(CommandStatus status, string commandId, Exception exception)
        {
            this.Status = status;
            this.CommandId = commandId;
            this._exception = exception;

            if (exception == null)
                return;

            var exceptionType = exception.GetType();

            this.ExceptionTypeName = string.Format("{0}, {1}",
                exceptionType.FullName,
                Path.GetFileNameWithoutExtension(exceptionType.Assembly.ManifestModule.FullyQualifiedName));
            this.ErrorMessage = exception.Message;
            this.ErrorData = exception.Data;

            var thinkNetException = exception as ThinkNetException;
            if (thinkNetException == null)
                this.ErrorCode = "-1";
            else
                this.ErrorCode = thinkNetException.MessageCode;
        }

        /// <summary>
        /// 获取处理命令的异常
        /// </summary>
        public virtual Exception GetInnerException()
        {
            if (_exception != null)
                return _exception;

            if (string.IsNullOrWhiteSpace(this.ExceptionTypeName))
                return null;

            try {
                var exceptionType = Type.GetType(this.ExceptionTypeName);
                var constructor = exceptionType.GetConstructor(new Type[] { typeof(string) });

                Exception exception;
                if (constructor == null) {
                    exception = Activator.CreateInstance(exceptionType) as Exception;
                }
                else {
                    exception = constructor.Invoke(new object[] { this.ErrorMessage }) as Exception;
                }

                if (exception != null && this.ErrorData != null && this.ErrorData.Count > 0) {
                    foreach (DictionaryEntry entry in this.ErrorData) {
                        exception.Data[entry.Key] = entry.Value;
                    }
                }

                var thinkNetException = exception as ThinkNetException;
                if (thinkNetException != null) {
                    thinkNetException.MessageCode = this.ErrorCode;
                    return thinkNetException;
                }

                return exception;
            }
            catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Overrides to return the command result info.
        /// </summary>
        public override string ToString()
        {
            return string.Format("[CommandId={0},Status={1},ExceptionTypeName={2},ErrorMessage={3}]",
                CommandId,
                Status,
                ExceptionTypeName,
                ErrorMessage);
        }

        /// <summary>
        /// 空的结果
        /// </summary>
        public static readonly CommandResult Empty = new CommandResult();
    }
}
