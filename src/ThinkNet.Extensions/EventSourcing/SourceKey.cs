
namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 溯源用的主键
    /// </summary>
    public struct SourceKey
    {
        private string sourceAssemblyName;
        private string sourceNamespace;
        private string sourceTypeName;
        private string sourceId;

        public SourceKey(string sourceId, string sourceTypeName)
            : this(sourceId, sourceTypeName, string.Empty, string.Empty)
        { }

        public SourceKey(string sourceId, string sourceTypeName, string sourceNamespace, string sourceAssemblyName)
        {
            this.sourceId = sourceId;
            this.sourceTypeName = sourceTypeName;
            this.sourceNamespace = sourceNamespace;
            this.sourceAssemblyName = sourceAssemblyName;
        }

        /// <summary>
        /// 程序集
        /// </summary>
        public string SourceAssemblyName { get { return this.sourceAssemblyName; } }
        /// <summary>
        /// 命名空间
        /// </summary>
        public string SourceNamespace { get { return this.sourceNamespace; }  }
        /// <summary>
        /// 类型名称
        /// </summary>
        public string SourceTypeName { get { return this.sourceTypeName; }  }

        /// <summary>
        /// 标识。
        /// </summary>
        public string SourceId { get { return this.sourceId; } }
    }
}
