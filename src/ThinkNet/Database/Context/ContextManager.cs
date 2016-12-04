using System;
using System.Configuration;

namespace ThinkNet.Database.Context
{
    /// <summary>
    /// <see cref="IContextManager"/> 的抽象实现类
    /// </summary>
    public class ContextManager : IContextManager
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected ContextManager()
            : this(null)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected ContextManager(string contextType)
        {
            this.Id = Guid.NewGuid();
            this.ContextType = contextType.IfEmpty(ConfigurationManager.AppSettings["thinkcfg.context_type"]).IfEmpty("web");
        }

        /// <summary>
        /// 标识
        /// </summary>
        public Guid Id
        {
            get;
            private set;
        }

        private string _contextType;
        /// <summary>
        /// 上下文类型
        /// </summary>
        protected internal string ContextType
        {
            get { return this._contextType; }
            set { this._contextType = value; }
        }

        private ICurrentContext _currentContext;
        /// <summary>
        /// 获取当前的上下文
        /// </summary>
        public ICurrentContext CurrentContext
        {
            get
            {
                if (_currentContext != null)
                    return _currentContext;

                switch (_contextType) {
                    case "web":
                        _currentContext = new WebContext(this);
                        break;
                    case "wcf":
                        _currentContext = new OperationContext(this);
                        break;
                    case "call":
                        _currentContext = new CallContext(this);
                        break;
                    case "thread":
                        _currentContext = new ThreadContext(this);
                        break;
                    default:
                        if (!string.IsNullOrEmpty(_contextType)) {
                            _currentContext = (ICurrentContext)Activator.CreateInstance(Type.GetType(_contextType), this);
                        }
                        break;
                }

                return _currentContext;
            }
        }
    }
}
