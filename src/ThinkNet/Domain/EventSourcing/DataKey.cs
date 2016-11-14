using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThinkNet.Domain.EventSourcing
{
    /// <summary>
    /// 表示一个完整的数据主键
    /// </summary>
    [Serializable]
    public struct DataKey : IEquatable<DataKey>
    {
        /// <summary>
        /// 空的主键
        /// </summary>
        public static readonly DataKey Empty = new DataKey();

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public DataKey(string str)
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
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public DataKey(object sourceId, Type sourceType)
            : this(sourceId.ToString(),
            sourceType.Namespace,
            sourceType.Name,
            sourceType.GetAssemblyName())
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public DataKey(string sourceId, string sourceNamespace, string sourceTypeName)
            : this(sourceId, sourceNamespace, sourceTypeName, null)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public DataKey(string sourceId, string sourceNamespace, string sourceTypeName, string sourceAssemblyName)
        {
            sourceId.NotNullOrWhiteSpace("sourceId");
            sourceNamespace.NotNullOrWhiteSpace("sourceNamespace");
            sourceTypeName.NotNullOrWhiteSpace("sourceTypeName");
            //sourceAssemblyName.NotNullOrWhiteSpace("sourceAssemblyName");

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
        /// 源标识。
        /// </summary>
        public string SourceId
        {
            get { return this.sourceId; }
            private set { this.sourceId = value; }
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
                sb.Append(", ").Append(this.AssemblyName);
            if (!string.IsNullOrWhiteSpace(this.SourceId))
                sb.Append("@").Append(this.SourceId);

            return sb.ToString();
        }

        /// <summary>
        /// 确定此实例是否与指定的对象相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(DataKey))
                return false;

            DataKey other = (DataKey)obj;

            return IsEqual(this, other);
        }

        /// <summary>
        /// 返回此实例的哈希代码。
        /// </summary>
        public override int GetHashCode()
        {
            var codes = new int[] {
                string.Concat(this.Namespace, ".", this.TypeName).GetHashCode(),
                this.AssemblyName.GetHashCode(),
                this.SourceId.GetHashCode()
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
        public static bool operator ==(DataKey left, DataKey right)
        {
            return IsEqual(left, right);
        }
        /// <summary>
        /// 判断是否不相等
        /// </summary>
        public static bool operator !=(DataKey left, DataKey right)
        {
            return !IsEqual(left, right);
        }

        private static bool IsEqual(DataKey left, DataKey right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null)) {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }


        #region IEquatable<SourceKey> 成员

        bool IEquatable<DataKey>.Equals(DataKey other)
        {
            return this.SourceId == other.SourceId &&
                this.TypeName == other.TypeName &&
                this.Namespace == other.Namespace &&
                this.AssemblyName == other.AssemblyName;
        }

        #endregion

        /// <summary>
        /// 将 <see cref="DataKey"/> 的字符串表示形式转换为它的等效的 <see cref="DataKey"/>。
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static DataKey Parse(string input)
        {
            return new DataKey(input);
        }
        /// <summary>
        /// 将 <see cref="DataKey"/> 的字符串表示形式转换为它的等效的 <see cref="DataKey"/>。一个指示转换是否成功的返回值。
        /// </summary>
        public static bool TryParse(string input, out DataKey result)
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
