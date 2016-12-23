using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示一个完整的源数据主键
    /// </summary>
    [Serializable]
    public struct SourceKey : IEquatable<SourceKey>, IUniquelyIdentifiable, ISerializable
    {
        /// <summary>
        /// 空的主键
        /// </summary>
        public static readonly SourceKey Empty = new SourceKey();

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public SourceKey(string str)
        {
            var match = Regex.Match(str, @"^([\w-\.]+)\.([\w-]+),\s?([\w-]+)@([\w-]+)$");
            if (!match.Success) {
                throw new FormatException(str);
            }

            this.@namespace = match.Groups[1].Value;
            this.typeName = match.Groups[2].Value;
            this.assemblyName = match.Groups[3].Value;
            this.uniqueId = match.Groups[4].Value;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public SourceKey(object sourceId, Type sourceType)
            : this(sourceId.ToString(),
            sourceType.Namespace,
            sourceType.Name,
            sourceType.GetAssemblyName())
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public SourceKey(string sourceId, string sourceTypeName)
        {
            var match = Regex.Match(sourceTypeName, @"^([\w-\.]+)\.([\w-]+),\s?([\w-]+)$");
            if (!match.Success) {
                throw new FormatException(sourceTypeName);
            }

            this.@namespace = match.Groups[1].Value;
            this.typeName = match.Groups[2].Value;
            this.assemblyName = match.Groups[3].Value;
            this.uniqueId = sourceId;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public SourceKey(string sourceId, string sourceNamespace, string sourceTypeName)
            : this(sourceId, sourceNamespace, sourceTypeName, null)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public SourceKey(string sourceId, string sourceNamespace, string sourceTypeName, string sourceAssemblyName)
        {
            sourceId.NotNullOrWhiteSpace("sourceId");
            sourceNamespace.NotNullOrWhiteSpace("sourceNamespace");
            sourceTypeName.NotNullOrWhiteSpace("sourceTypeName");
            //sourceAssemblyName.NotNullOrWhiteSpace("sourceAssemblyName");

            this.uniqueId = sourceId;
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
        private string uniqueId;
        /// <summary>
        /// 源标识。
        /// </summary>
        public string Id
        {
            get { return this.uniqueId; }
            private set { this.uniqueId = value; }
        }

        /// <summary>
        /// 输出该结构的字符串格式。
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(this.Namespace))
                sb.Append(this.Namespace).Append(".");
            if (!string.IsNullOrWhiteSpace(this.TypeName))
                sb.Append(this.TypeName);
            if (!string.IsNullOrWhiteSpace(this.AssemblyName))
                sb.Append(",").Append(this.AssemblyName);
            if (!string.IsNullOrWhiteSpace(this.Id))
                sb.Append("@").Append(this.Id);

            return sb.ToString();
        }

        /// <summary>
        /// 确定此实例是否与指定的对象相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(SourceKey))
                return false;

            SourceKey other = (SourceKey)obj;

            return IsEqual(this, other);
        }

        /// <summary>
        /// 返回此实例的哈希代码。
        /// </summary>
        public override int GetHashCode()
        {
            var codes = new int[] {
                this.AssemblyName.GetHashCode(),
                this.Namespace.GetHashCode(),
                this.TypeName.GetHashCode(),
                this.Id.GetHashCode()
            };
            return codes.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 获取源类型
        /// </summary>
        public Type GetSourceType()
        {
            string typeFullName = this.GetSourceTypeFullName();
            return Type.GetType(typeFullName);
        }
        /// <summary>
        /// 获取源类型完整名称但不包括程序集名称。
        /// </summary>
        public string GetSourceTypeName()
        {
            return string.Concat(this.Namespace, ".", this.TypeName);
        }
        /// <summary>
        /// 获取源类型完整名称且包括程序集名称。
        /// </summary>
        public string GetSourceTypeFullName()
        {
            return string.Concat(this.Namespace, ".", this.TypeName, ", ", this.AssemblyName);
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
            return this.Id == other.Id &&
                this.TypeName == other.TypeName &&
                this.Namespace == other.Namespace &&
                this.AssemblyName == other.AssemblyName;
        }

        #endregion

        /// <summary>
        /// 将 <see cref="SourceKey"/> 的字符串表示形式转换为它的等效的 <see cref="SourceKey"/>。
        /// </summary>
        public static SourceKey Parse(string input)
        {
            return new SourceKey(input);
        }
        /// <summary>
        /// 将 <see cref="SourceKey"/> 的字符串表示形式转换为它的等效的 <see cref="SourceKey"/>。一个指示转换是否成功的返回值。
        /// </summary>
        public static bool TryParse(string input, out SourceKey result)
        {
            if (!Regex.IsMatch(input, @"^([\w-\.]+)\.([\w-]+),\s([\w-]+)@([\w-]+)$")) {
                result = Empty;
                return false;
            }

            result = Parse(input);
            return true;
        }


        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", this.Id);
            info.AddValue("AssemblyName", this.AssemblyName);
            info.AddValue("Namespace", this.Namespace);
            info.AddValue("TypeName", this.TypeName);
        }
    }
}
