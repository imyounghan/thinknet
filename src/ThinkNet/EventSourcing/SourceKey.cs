using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 溯源用的主键
    /// </summary>
    [Serializable]
    public struct SourceKey : IEquatable<SourceKey>
    {
        public static readonly SourceKey Empty = new SourceKey();

        public SourceKey(string str)
        {
            var match = Regex.Match(str, @"^([\w-\.]+)\.([\w-]+),\s([\w-]+)@([\w-]+)$");
            if (!match.Success) {
                throw new FormatException(str);
            }

            this.@namespace = match.Groups[1].Value;
            this.typeName = match.Groups[2].Value;
            this.assemblyName = match.Groups[3].Value;
            this.sourceId = match.Groups[4].Value;
        }

        public SourceKey(object sourceId, Type sourceType)
            : this(sourceId.ToString(),
            sourceType.Namespace,
            sourceType.Name,
            Path.GetFileNameWithoutExtension(sourceType.Assembly.ManifestModule.FullyQualifiedName))
        { }

        public SourceKey(string sourceId, string sourceNamespace, string sourceTypeName)
            : this(sourceId, sourceNamespace, sourceTypeName, string.Empty)
        { }

        public SourceKey(string sourceId, string sourceNamespace, string sourceTypeName, string sourceAssemblyName)
        {
            sourceId.NotNullOrWhiteSpace("sourceId");
            sourceId.NotNullOrWhiteSpace("sourceNamespace");
            sourceId.NotNullOrWhiteSpace("sourceTypeName");
            sourceId.NotNullOrWhiteSpace("sourceAssemblyName");

            this.sourceId = sourceId;
            this.@namespace = sourceNamespace;
            this.typeName = sourceTypeName;            
            this.assemblyName = sourceAssemblyName;
        }

        private string assemblyName;
        /// <summary>
        /// 程序集
        /// </summary>
        public string AssemblyName
        {
            get { return this.assemblyName; }
            private set { this.assemblyName = value; }
        }
        private string @namespace;
        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace
        {
            get { return this.@namespace; }
            private set { this.@namespace = value; }
        }
        private string typeName;
        /// <summary>
        /// 类型名称(不包含全名空间)
        /// </summary>
        public string TypeName
        {
            get { return this.typeName; }
            private set { this.typeName = value; }
        }
        private string sourceId;
        /// <summary>
        /// 标识。
        /// </summary>
        public string SourceId
        {
            get { return this.sourceId; }
            private set { this.sourceId = value; }
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(this.Namespace))
                sb.Append(this.Namespace).Append(".");
            if (!string.IsNullOrWhiteSpace(this.TypeName))
                sb.Append(this.TypeName);
            if (!string.IsNullOrWhiteSpace(this.AssemblyName))
                sb.Append(", ").Append(this.AssemblyName);
            if (!string.IsNullOrWhiteSpace(this.SourceId))
                sb.Append("@").Append(this.SourceId);

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(SourceKey))
                return false;

            SourceKey other = (SourceKey)obj;

            return IsEqual(this, other);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// 判断是否相等
        /// </summary>
        public static bool operator ==(SourceKey left, SourceKey right)
        {
            return IsEqual(left, right);
        }
        /// <summary>
        /// 判断是否不相等
        /// </summary>
        public static bool operator !=(SourceKey left, SourceKey right)
        {
            return !IsEqual(left, right);
        }

        private static bool IsEqual(SourceKey left, SourceKey right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null)) {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }


        #region IEquatable<SourceKey> 成员

        bool IEquatable<SourceKey>.Equals(SourceKey other)
        {
            return this.SourceId == other.SourceId &&
                this.TypeName == other.TypeName &&
                this.Namespace == other.Namespace &&
                this.AssemblyName == other.AssemblyName;
        }

        #endregion


        public static SourceKey Parse(string input)
        {
            return new SourceKey(input);
        }

        public static bool TryParse(string input, out SourceKey result)
        {
            if (!Regex.IsMatch(input, @"^([\w-\.]+)\.([\w-]+),\s([\w-]+)@([\w-]+)$")) {
                result = Empty;
                return false;
            }

            result = Parse(input);
            return true;
        }
    }
}
