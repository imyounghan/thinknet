using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace ThinkNet
{
    /// <summary>
    /// 表示这是一个日志管理器
    /// </summary>
    public class LogManager
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        public interface ILogger
        {
            /// <summary>
            /// 是否启用Debug日志
            /// </summary>
            bool IsDebugEnabled { get; }
            /// <summary>
            /// 是否启用Info日志
            /// </summary>
            bool IsInfoEnabled { get; }
            /// <summary>
            /// 是否启用Warn日志
            /// </summary>
            bool IsWarnEnabled { get; }
            /// <summary>
            /// 是否启用Error日志
            /// </summary>
            bool IsErrorEnabled { get; }
            /// <summary>
            /// 是否启用Fatal日志
            /// </summary>
            bool IsFatalEnabled { get; }

            ///// <summary>
            ///// 写日志。
            ///// </summary>
            //void Debug(object message);
            /// <summary>
            /// 写日志。
            /// </summary>
            void Debug(object message, Exception exception = null);
            /// <summary>
            /// 写日志。
            /// </summary>
            void DebugFormat(string format, params object[] args);


            ///// <summary>
            ///// 写日志。
            ///// </summary>
            //void Info(object message);
            /// <summary>
            /// 写日志。
            /// </summary>
            void Info(object message, Exception exception = null);
            /// <summary>
            /// 写日志。
            /// </summary>
            void InfoFormat(string format, params object[] args);


            ///// <summary>
            ///// 写日志。
            ///// </summary>
            //void Warn(object message);
            /// <summary>
            /// 写日志。
            /// </summary>
            void Warn(object message, Exception exception = null);
            /// <summary>
            /// 写日志。
            /// </summary>
            void WarnFormat(string format, params object[] args);


            ///// <summary>
            ///// 写日志。
            ///// </summary>
            //void Error(object message);
            /// <summary>
            /// 写日志。
            /// </summary>
            void Error(object message, Exception exception = null);
            /// <summary>
            /// 写日志。
            /// </summary>
            void ErrorFormat(string format, params object[] args);

            ///// <summary>
            ///// 写日志。
            ///// </summary>
            //void Fatal(object message);
            /// <summary>
            /// 写日志。
            /// </summary>
            void Fatal(object message, Exception exception = null);
            /// <summary>
            /// 写日志。
            /// </summary>
            void FatalFormat(string format, params object[] args);
        }

        class EmptyLogger : ILogger
        {
            public static readonly ILogger Instance = new EmptyLogger();

            public bool IsDebugEnabled
            {
                get { return false; }
            }

            public bool IsInfoEnabled
            {
                get { return false; }
            }

            public bool IsWarnEnabled
            {
                get { return false; }
            }

            public bool IsErrorEnabled
            {
                get { return false; }
            }

            public bool IsFatalEnabled
            {
                get { return false; }
            }

            public void Debug(object message, Exception exception = null)
            { }

            public void DebugFormat(string format, params object[] args)
            { }

            public void Info(object message, Exception exception = null)
            { }

            public void InfoFormat(string format, params object[] args)
            { }

            public void Warn(object message, Exception exception = null)
            { }

            public void WarnFormat(string format, params object[] args)
            { }

            public void Error(object message, Exception exception = null)
            { }

            public void ErrorFormat(string format, params object[] args)
            { }

            public void Fatal(object message, Exception exception = null)
            { }

            public void FatalFormat(string format, params object[] args)
            { }
        }

        class DefaultLogger : ILogger
        {
            [Flags]
            enum Priority
            {
                DEBUG = 1,
                INFO = 2,
                WARN = 4,
                ERROR = 8,
                FATAL = 16
            }

            public static readonly ILogger Instance = new DefaultLogger();

            readonly static string LogAppender;
            readonly static Priority LogPriority;

            static DefaultLogger()
            {
                LogAppender = ConfigurationManager.AppSettings["thinkcfg.log_appender"].IfEmpty("FILE").ToLower();
                LogPriority = GetLogPriority(ConfigurationManager.AppSettings["thinkcfg.log_priority"].IfEmpty("OFF"));

                if ((short)LogPriority == -1)
                    return;

                if (LogAppender == "all" || LogAppender == "console") {
                    Trace.Listeners.Add(new ConsoleTraceListener(false));
                }
                if (LogAppender == "all" || LogAppender == "file") {
                    var logFile = CreateFile();
                    Trace.Listeners.Add(new TextWriterTraceListener(logFile));
                    Trace.AutoFlush = true;
                }
            }

            private static string GetMapPath(string fileName)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
                string binPath = string.IsNullOrEmpty(relativeSearchPath) ? baseDir : Path.Combine(baseDir, relativeSearchPath);
                return Path.Combine(binPath, fileName);
            }

            private static string CreateFile()
            {
                string today = DateTime.Today.ToString("yyyyMMdd");
                string filename = GetMapPath(string.Concat("log\\log_", today, ".txt"));
                int fileIndex = 0;

                while (true) {
                    if (!File.Exists(filename)) {
                        return filename;
                    }
                    filename = GetMapPath(string.Concat("log\\log_", today, "_", ++fileIndex, ".txt"));
                }
            }

            static Priority GetLogPriority(string priority)
            {
                switch (priority.ToLower()) {
                    case "debug":
                        priority += "|info|warn|error|fatal";
                        break;
                    case "info":
                        priority += "|warn|error|fatal";
                        break;
                    case "warn":
                        priority += "|error|fatal";
                        break;
                    case "error":
                        priority += "|fatal";
                        break;
                    case "fatal":
                        break;
                    default:
                        return (Priority)(-1);
                }

                Priority logPriority = (Priority)(-1);

                priority.Split('|').ForEach(item => {
                    Priority temp;
                    if (!Enum.TryParse(item, true, out temp))
                        return;

                    if (logPriority == (Priority)(-1)) {
                        logPriority = temp;
                        return;
                    }


                    logPriority |= temp;
                });

                return logPriority;
            }

            static bool IsContain(Priority priority, Priority comparer)
            {
                return (priority & comparer) == comparer;
            }

            public bool IsDebugEnabled
            {
                get { return IsContain(LogPriority, Priority.DEBUG); }
            }

            public bool IsInfoEnabled
            {
                get { return IsContain(LogPriority, Priority.INFO); }
            }

            public bool IsWarnEnabled
            {
                get { return IsContain(LogPriority, Priority.WARN); }
            }

            public bool IsErrorEnabled
            {
                get { return IsContain(LogPriority, Priority.ERROR); }
            }

            public bool IsFatalEnabled
            {
                get { return IsContain(LogPriority, Priority.FATAL); }
            }



            void Write(Priority logpriority, string message, Exception exception)
            {
                if (!IsContain(LogPriority, logpriority))
                    return;

                StringBuilder log = new StringBuilder()
                    .Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    .AppendFormat(" {0} [{1}]", logpriority, Thread.CurrentThread.Name.IfEmpty(Thread.CurrentThread.ManagedThreadId.ToString().PadRight(5)));
                if (!string.IsNullOrWhiteSpace(message)) {
                    log.Append(" Message:").Append(message);
                }
                if (exception != null) {
                    log.Append(" Exception:").Append(exception);
                    if (exception.InnerException != null) {
                        log.AppendLine().Append("InnerException:").Append(exception.InnerException);
                    }
                }

                Trace.WriteLine(log.ToString(), "ThinkNet");
            }

            public void Debug(object message, Exception exception = null)
            {
                Write(Priority.DEBUG, message.ToString(), exception);
            }

            public void DebugFormat(string format, params object[] args)
            {
                Write(Priority.DEBUG, string.Format(format, args), null);
            }

            public void Info(object message, Exception exception = null)
            {
                Write(Priority.INFO, message.ToString(), exception);
            }

            public void InfoFormat(string format, params object[] args)
            {
                Write(Priority.INFO, string.Format(format, args), null);
            }

            public void Warn(object message, Exception exception = null)
            {
                Write(Priority.WARN, message.ToString(), exception);
            }

            public void WarnFormat(string format, params object[] args)
            {
                Write(Priority.WARN, string.Format(format, args), null);            
            }

            public void Error(object message, Exception exception = null)
            {
                Write(Priority.ERROR, message.ToString(), exception);
            }

            public void ErrorFormat(string format, params object[] args)
            {
                Write(Priority.ERROR, string.Format(format, args), null);
            }

            public void Fatal(object message, Exception exception = null)
            {
                Write(Priority.FATAL, message.ToString(), exception);
            }

            public void FatalFormat(string format, params object[] args)
            {
                Write(Priority.FATAL, string.Format(format, args), null);
            }
        }
        
        class Log4NetLogger : ILogger
        {
            private static readonly Type ILogType;
            private static readonly Func<object, bool> IsErrorEnabledDelegate;
            private static readonly Func<object, bool> IsFatalEnabledDelegate;
            private static readonly Func<object, bool> IsDebugEnabledDelegate;
            private static readonly Func<object, bool> IsInfoEnabledDelegate;
            private static readonly Func<object, bool> IsWarnEnabledDelegate;

            private static readonly Action<object, object> ErrorDelegate;
            private static readonly Action<object, object, Exception> ErrorExceptionDelegate;
            private static readonly Action<object, string, object[]> ErrorFormatDelegate;

            private static readonly Action<object, object> FatalDelegate;
            private static readonly Action<object, object, Exception> FatalExceptionDelegate;
            private static readonly Action<object, string, object[]> FatalFormatDelegate;

            private static readonly Action<object, object> DebugDelegate;
            private static readonly Action<object, object, Exception> DebugExceptionDelegate;
            private static readonly Action<object, string, object[]> DebugFormatDelegate;

            private static readonly Action<object, object> InfoDelegate;
            private static readonly Action<object, object, Exception> InfoExceptionDelegate;
            private static readonly Action<object, string, object[]> InfoFormatDelegate;

            private static readonly Action<object, object> WarnDelegate;
            private static readonly Action<object, object, Exception> WarnExceptionDelegate;
            private static readonly Action<object, string, object[]> WarnFormatDelegate;

            private readonly object logger;

            static Log4NetLogger()
            {
                ILogType = Type.GetType("log4net.ILog, log4net");

                IsErrorEnabledDelegate = GetPropertyGetter("IsErrorEnabled");
                IsFatalEnabledDelegate = GetPropertyGetter("IsFatalEnabled");
                IsDebugEnabledDelegate = GetPropertyGetter("IsDebugEnabled");
                IsInfoEnabledDelegate = GetPropertyGetter("IsInfoEnabled");
                IsWarnEnabledDelegate = GetPropertyGetter("IsWarnEnabled");
                ErrorDelegate = GetMethodCallForMessage("Error");
                ErrorExceptionDelegate = GetMethodCallForMessageException("Error");
                ErrorFormatDelegate = GetMethodCallForMessageFormat("ErrorFormat");

                FatalDelegate = GetMethodCallForMessage("Fatal");
                FatalExceptionDelegate = GetMethodCallForMessageException("Fatal");
                FatalFormatDelegate = GetMethodCallForMessageFormat("FatalFormat");

                DebugDelegate = GetMethodCallForMessage("Debug");
                DebugExceptionDelegate = GetMethodCallForMessageException("Debug");
                DebugFormatDelegate = GetMethodCallForMessageFormat("DebugFormat");

                InfoDelegate = GetMethodCallForMessage("Info");
                InfoExceptionDelegate = GetMethodCallForMessageException("Info");
                InfoFormatDelegate = GetMethodCallForMessageFormat("InfoFormat");

                WarnDelegate = GetMethodCallForMessage("Warn");
                WarnExceptionDelegate = GetMethodCallForMessageException("Warn");
                WarnFormatDelegate = GetMethodCallForMessageFormat("WarnFormat");
            }

            private static Func<object, bool> GetPropertyGetter(string propertyName)
            {
                ParameterExpression funcParam = Expression.Parameter(typeof(object), "l");
                Expression convertedParam = Expression.Convert(funcParam, ILogType);
                Expression property = Expression.Property(convertedParam, propertyName);
                return (Func<object, bool>)Expression.Lambda(property, funcParam).Compile();
            }

            private static Action<object, object> GetMethodCallForMessage(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof(object), "l");
                ParameterExpression messageParam = Expression.Parameter(typeof(object), "o");
                Expression convertedParam = Expression.Convert(loggerParam, ILogType);
                MethodCallExpression methodCall = Expression.Call(convertedParam, ILogType.GetMethod(methodName, new[] { typeof(object) }), messageParam);
                return (Action<object, object>)Expression.Lambda(methodCall, new[] { loggerParam, messageParam }).Compile();
            }

            private static Action<object, object, Exception> GetMethodCallForMessageException(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof(object), "l");
                ParameterExpression messageParam = Expression.Parameter(typeof(object), "o");
                ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception), "e");
                Expression convertedParam = Expression.Convert(loggerParam, ILogType);
                MethodCallExpression methodCall = Expression.Call(convertedParam, ILogType.GetMethod(methodName, new[] { typeof(object), typeof(Exception) }), messageParam, exceptionParam);
                return (Action<object, object, Exception>)Expression.Lambda(methodCall, new[] { loggerParam, messageParam, exceptionParam }).Compile();
            }

            private static Action<object, string, object[]> GetMethodCallForMessageFormat(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof(object), "l");
                ParameterExpression formatParam = Expression.Parameter(typeof(string), "f");
                ParameterExpression parametersParam = Expression.Parameter(typeof(object[]), "p");
                Expression convertedParam = Expression.Convert(loggerParam, ILogType);
                MethodCallExpression methodCall = Expression.Call(convertedParam, ILogType.GetMethod(methodName, new[] { typeof(string), typeof(object[]) }), formatParam, parametersParam);
                return (Action<object, string, object[]>)Expression.Lambda(methodCall, new[] { loggerParam, formatParam, parametersParam }).Compile();
            }

            public Log4NetLogger(object logger)
            {
                this.logger = logger;
            }

            public bool IsErrorEnabled
            {
                get { return IsErrorEnabledDelegate(logger); }
            }

            public bool IsFatalEnabled
            {
                get { return IsFatalEnabledDelegate(logger); }
            }

            public bool IsDebugEnabled
            {
                get { return IsDebugEnabledDelegate(logger); }
            }

            public bool IsInfoEnabled
            {
                get { return IsInfoEnabledDelegate(logger); }
            }

            public bool IsWarnEnabled
            {
                get { return IsWarnEnabledDelegate(logger); }
            }

            public void Error(object message, Exception exception)
            {
                if (!IsErrorEnabled)
                    return;

                if (exception == null)
                    ErrorDelegate(logger, message);
                else
                    ErrorExceptionDelegate(logger, message, exception);
            }

            public void ErrorFormat(string format, params object[] args)
            {
                if (IsErrorEnabled)
                    ErrorFormatDelegate(logger, format, args);
            }


            public void Fatal(object message, Exception exception)
            {
                if (!IsFatalEnabled)
                    return;

                if (exception == null)
                    FatalDelegate(logger, message);
                else
                    FatalExceptionDelegate(logger, message, exception);
            }

            public void FatalFormat(string format, params object[] args)
            {
                if (IsFatalEnabled)
                    FatalFormatDelegate(logger, format, args);
            }


            public void Debug(object message, Exception exception)
            {
                if (!IsDebugEnabled)
                    return;

                if (exception == null)
                    DebugDelegate(logger, message);
                else
                    DebugExceptionDelegate(logger, message, exception);
            }

            public void DebugFormat(string format, params object[] args)
            {
                if (IsDebugEnabled)
                    DebugFormatDelegate(logger, format, args);
            }


            public void Info(object message, Exception exception)
            {
                if (!IsInfoEnabled)
                    return;

                if (exception == null)
                    InfoDelegate(logger, message);
                else
                    InfoExceptionDelegate(logger, message, exception);
            }

            public void InfoFormat(string format, params object[] args)
            {
                if (IsInfoEnabled)
                    InfoFormatDelegate(logger, format, args);
            }


            public void Warn(object message, Exception exception)
            {
                if (!IsWarnEnabled)
                    return;

                if (exception == null)
                    WarnDelegate(logger, message);
                else
                    WarnExceptionDelegate(logger, message, exception);
            }

            public void WarnFormat(string format, params object[] args)
            {
                if (IsWarnEnabled)
                    WarnFormatDelegate(logger, format, args);
            }
        }

        class Log4NetLoggerFactory
        {
            public readonly static Log4NetLoggerFactory Instance = new Log4NetLoggerFactory();

            private readonly System.Type LogManagerType;
            private readonly Func<string, object> GetLoggerByNameDelegate;
            private readonly Func<System.Type, object> GetLoggerByTypeDelegate;
            public Log4NetLoggerFactory()
            {
                this.LogManagerType = Type.GetType("log4net.LogManager, log4net");
                this.GetLoggerByNameDelegate = GetGetLoggerMethodCall<string>();
                this.GetLoggerByTypeDelegate = GetGetLoggerMethodCall<System.Type>();
            }

            private Func<TParameter, object> GetGetLoggerMethodCall<TParameter>()
            {
                var method = LogManagerType.GetMethod("GetLogger", new[] { typeof(TParameter) });
                ParameterExpression resultValue;
                ParameterExpression keyParam = Expression.Parameter(typeof(TParameter), "key");
                MethodCallExpression methodCall = Expression.Call(null, method, new Expression[] { resultValue = keyParam });
                return Expression.Lambda<Func<TParameter, object>>(methodCall, new[] { resultValue }).Compile();
            }
            
            public ILogger Create(string name)
            {
                return new Log4NetLogger(GetLoggerByNameDelegate(name));
            }

            public ILogger Create(Type type)
            {
                return new Log4NetLogger(GetLoggerByTypeDelegate(type));
            }
        }

        private static bool existLog4Net;
        static LogManager()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string binPath = string.IsNullOrEmpty(relativeSearchPath) ? baseDir : Path.Combine(baseDir, relativeSearchPath);
            string log4NetDllPath = string.IsNullOrEmpty(binPath) ? "log4net.dll" : Path.Combine(binPath, "log4net.dll");

            existLog4Net = File.Exists(log4NetDllPath) || AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "log4net");

            if(existLog4Net) {
                Composition = "log4net";
            }

            loggers = new ConcurrentDictionary<string, ILogger>();
        }

        /// <summary>
        /// 表示这是一个默认的日志程序。
        /// </summary>
        public static ILogger Default { get { return GetLogger("ThinkNet"); } }

        /// <summary>
        /// 当前使用的组件
        /// </summary>
        public static string Composition { get; private set; }


        private readonly static ConcurrentDictionary<string, ILogger> loggers;
        /// <summary>
        /// 通过名称获取一个日志
        /// </summary>
        public static ILogger GetLogger(string name)
        {
            name.NotNullOrWhiteSpace("name");

            if (loggerFactory != null || existLog4Net) {
                return loggers.GetOrAdd(name, () => CreateLogger(name, null));
            }

            if(name.Equals("ThinkNet", StringComparison.CurrentCulture))
                return DefaultLogger.Instance;

            return EmptyLogger.Instance;
        }
        /// <summary>
        /// 通过类型获取一个日志
        /// </summary>
        public static ILogger GetLogger(Type type)
        {
            type.NotNull("type");

            if (loggerFactory != null || existLog4Net) {
                return loggers.GetOrAdd(type.FullName, () => CreateLogger(null, type));
            }

            return EmptyLogger.Instance;
        }

        private static ILogger CreateLogger(string name, Type type)
        {
            if (loggerFactory != null) {
                return loggerFactory.Invoke(name, type);
            }

            if (!string.IsNullOrEmpty(name))
                return Log4NetLoggerFactory.Instance.Create(name);

            return Log4NetLoggerFactory.Instance.Create(type);
        }

        private static Func<string, Type, ILogger> loggerFactory;
        /// <summary>
        /// 设置日志工厂
        /// </summary>
        public static void SetLoggerFactory(Func<string, Type, ILogger> factory)
        {
            if (loggerFactory != null)
                return;

            factory.NotNull("factory");

            Composition = "outside";

            Interlocked.CompareExchange(ref loggerFactory, factory, null);
        }
    }

    /// <summary>
    /// <see cref="LogManager.ILogger"/>的扩展方法类
    /// </summary>
    public static class LogExtensions
    {
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Debug(this LogManager.ILogger log, Exception ex)
        {
            log.Debug(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Debug(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Debug(string.Format(format, args), ex);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Info(this LogManager.ILogger log, Exception ex)
        {
            log.Info(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Info(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Info(string.Format(format, args), ex);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Warn(this LogManager.ILogger log, Exception ex)
        {
            log.Warn(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Warn(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Warn(string.Format(format, args), ex);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Error(this LogManager.ILogger log, Exception ex)
        {
            log.Error(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Error(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Error(string.Format(format, args), ex);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Fatal(this LogManager.ILogger log, Exception ex)
        {
            log.Fatal(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Fatal(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Fatal(string.Format(format, args), ex);
        }
    }
}
