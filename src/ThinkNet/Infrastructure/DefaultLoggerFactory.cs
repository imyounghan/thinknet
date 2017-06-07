

namespace ThinkNet.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading;

    public class DefaultLoggerFactory : ILoggerFactory
    {
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



            #region ILogger 成员


            public void Debug(object message)
            { }

            public void Debug(object message, Exception exception)
            { }

            public void DebugFormat(string format, params object[] args)
            { }

            public void Info(object message)
            { }

            public void Info(object message, Exception exception)
            { }

            public void InfoFormat(string format, params object[] args)
            { }

            public void Warn(object message)
            { }

            public void Warn(object message, Exception exception)
            {
                throw new NotImplementedException();
            }

            public void WarnFormat(string format, params object[] args)
            { }

            public void Error(object message)
            { }

            public void Error(object message, Exception exception)
            { }

            public void ErrorFormat(string format, params object[] args)
            { }

            public void Fatal(object message)
            { }

            public void Fatal(object message, Exception exception)
            { }

            public void FatalFormat(string format, params object[] args)
            { }

            #endregion
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

                if((short)LogPriority == -1)
                    return;

                if(LogAppender == "all" || LogAppender == "console") {
                    Trace.Listeners.Add(new ConsoleTraceListener(false));
                }
                if(LogAppender == "all" || LogAppender == "file") {
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

                while(true) {
                    if(!File.Exists(filename)) {
                        return filename;
                    }
                    filename = GetMapPath(string.Concat("log\\log_", today, "_", ++fileIndex, ".txt"));
                }
            }

            static Priority GetLogPriority(string priority)
            {
                switch(priority.ToLower()) {
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
                    if(!Enum.TryParse(item, true, out temp))
                        return;

                    if(logPriority == (Priority)(-1)) {
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
                if(!IsContain(LogPriority, logpriority))
                    return;

                StringBuilder log = new StringBuilder()
                    .Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    .AppendFormat(" {0} [{1}]", logpriority, Thread.CurrentThread.Name.IfEmpty(Thread.CurrentThread.ManagedThreadId.ToString().PadRight(5)));
                if(!string.IsNullOrWhiteSpace(message)) {
                    log.Append(" Message:").Append(message);
                }
                if(exception != null) {
                    log.Append(" Exception:").Append(exception);
                    if(exception.InnerException != null) {
                        log.AppendLine().Append("InnerException:").Append(exception.InnerException);
                    }
                }

                Trace.WriteLine(log.ToString(), "ThinkNet");
            }



            #region ILogger 成员


            public void Debug(object message)
            {
                this.Debug(message, (Exception)null);
            }

            public void Debug(object message, Exception exception)
            {
                Write(Priority.DEBUG, message.ToString(), exception);
            }

            public void DebugFormat(string format, params object[] args)
            {
                Write(Priority.DEBUG, string.Format(format, args), null);
            }

            public void Info(object message)
            {
                this.Info(message, (Exception)null);
            }

            public void Info(object message, Exception exception)
            {
                Write(Priority.INFO, message.ToString(), exception);
            }

            public void InfoFormat(string format, params object[] args)
            {
                Write(Priority.INFO, string.Format(format, args), null);
            }

            public void Warn(object message)
            {
                this.Warn(message, (Exception)null);
            }

            public void Warn(object message, Exception exception)
            {
                Write(Priority.WARN, message.ToString(), exception);
            }

            public void WarnFormat(string format, params object[] args)
            {
                Write(Priority.WARN, string.Format(format, args), null);
            }

            public void Error(object message)
            {
                this.Error(message, (Exception)null);
            }

            public void Error(object message, Exception exception)
            {
                Write(Priority.ERROR, message.ToString(), exception);
            }

            public void ErrorFormat(string format, params object[] args)
            {
                Write(Priority.ERROR, string.Format(format, args), null);
            }

            public void Fatal(object message)
            {
                this.Fatal(message, (Exception)null);
            }

            public void Fatal(object message, Exception exception)
            {
                Write(Priority.FATAL, message.ToString(), exception);
            }

            public void FatalFormat(string format, params object[] args)
            {
                Write(Priority.FATAL, string.Format(format, args), null);
            }

            #endregion
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

            public void Error(object message)
            {
                this.Error(message, (Exception)null);
            }

            public void Error(object message, Exception exception)
            {
                if(!IsErrorEnabled)
                    return;

                if(exception == null)
                    ErrorDelegate(logger, message);
                else
                    ErrorExceptionDelegate(logger, message, exception);
            }

            public void ErrorFormat(string format, params object[] args)
            {
                if(IsErrorEnabled)
                    ErrorFormatDelegate(logger, format, args);
            }


            public void Fatal(object message)
            {
                this.Fatal(message, (Exception)null);
            }

            public void Fatal(object message, Exception exception)
            {
                if(!IsFatalEnabled)
                    return;

                if(exception == null)
                    FatalDelegate(logger, message);
                else
                    FatalExceptionDelegate(logger, message, exception);
            }

            public void FatalFormat(string format, params object[] args)
            {
                if(IsFatalEnabled)
                    FatalFormatDelegate(logger, format, args);
            }

            public void Debug(object message)
            {
                this.Debug(message, (Exception)null);
            }

            public void Debug(object message, Exception exception)
            {
                if(!IsDebugEnabled)
                    return;

                if(exception == null)
                    DebugDelegate(logger, message);
                else
                    DebugExceptionDelegate(logger, message, exception);
            }

            public void DebugFormat(string format, params object[] args)
            {
                if(IsDebugEnabled)
                    DebugFormatDelegate(logger, format, args);
            }

            public void Info(object message)
            {
                this.Info(message, (Exception)null);
            }

            public void Info(object message, Exception exception)
            {
                if(!IsInfoEnabled)
                    return;

                if(exception == null)
                    InfoDelegate(logger, message);
                else
                    InfoExceptionDelegate(logger, message, exception);
            }

            public void InfoFormat(string format, params object[] args)
            {
                if(IsInfoEnabled)
                    InfoFormatDelegate(logger, format, args);
            }

            public void Warn(object message)
            {
                this.Warn(message, (Exception)null);
            }

            public void Warn(object message, Exception exception)
            {
                if(!IsWarnEnabled)
                    return;

                if(exception == null)
                    WarnDelegate(logger, message);
                else
                    WarnExceptionDelegate(logger, message, exception);
            }

            public void WarnFormat(string format, params object[] args)
            {
                if(IsWarnEnabled)
                    WarnFormatDelegate(logger, format, args);
            }
        }


        private readonly static bool existLog4Net;

        private readonly static Type LogManagerType;
        private readonly static Func<string, object> GetLoggerByNameDelegate;
        private readonly static Func<Type, object> GetLoggerByTypeDelegate;

        static DefaultLoggerFactory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string binPath = string.IsNullOrEmpty(relativeSearchPath) ? baseDir : Path.Combine(baseDir, relativeSearchPath);
            string log4NetDllPath = string.IsNullOrEmpty(binPath) ? "log4net.dll" : Path.Combine(binPath, "log4net.dll");

            existLog4Net = File.Exists(log4NetDllPath) || AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "log4net");

            if (existLog4Net)
            {
                LogManagerType = Type.GetType("log4net.LogManager, log4net");
                GetLoggerByNameDelegate = GetGetLoggerMethodCall<string>();
                GetLoggerByTypeDelegate = GetGetLoggerMethodCall<Type>();
            }
        }

        private static Func<TParameter, object> GetGetLoggerMethodCall<TParameter>()
        {
            var method = LogManagerType.GetMethod("GetLogger", new[] { typeof(TParameter) });
            ParameterExpression resultValue;
            ParameterExpression keyParam = Expression.Parameter(typeof(TParameter), "key");
            MethodCallExpression methodCall = Expression.Call(null, method, new Expression[] { resultValue = keyParam });
            return Expression.Lambda<Func<TParameter, object>>(methodCall, new[] { resultValue }).Compile();
        }


        private readonly ConcurrentDictionary<string, ILogger> loggers;

        public DefaultLoggerFactory()
        {
            this.loggers = new ConcurrentDictionary<string, ILogger>();   
        }
        #region ILoggerFactory 成员

        public ILogger GetOrCreate(string name)
        {
            name.NotNullOrWhiteSpace("name");

            if( existLog4Net) {
                return loggers.GetOrAdd(name, () => new Log4NetLogger(GetLoggerByNameDelegate(name)));
            }

            if(name.Equals("ThinkNet", StringComparison.CurrentCulture))
                return DefaultLogger.Instance;

            return EmptyLogger.Instance;
        }

        public ILogger GetOrCreate(Type type)
        {
            type.NotNull("type");

            if(existLog4Net) {
                return loggers.GetOrAdd(type.FullName, () => new Log4NetLogger(GetLoggerByTypeDelegate(type)));
            }

            return EmptyLogger.Instance;
        }

        #endregion
    }
}
