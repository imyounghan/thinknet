using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示一个要发送的信件
    /// </summary>
    [DataContract]
    [Serializable]
    public class Envelope
    {
        //public Envelope()
        //{ }

        //public Envelope(string body, Type type)
        //{
        //    this.Body = body;

        //    this.Namespace = type.Namespace;
        //    this.TypeName = type.Name;
        //    this.AssemblyName = Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName);

        //    TypeMaps.TryAdd(type.FullName, type);
        //}
        [DataMember(Name = "body")]
        public object Body { get; set; }
        //[DataMember(Name = "typeName")]
        //public string TypeName { get; set; }
        //[DataMember(Name = "namespace")]
        //public string Namespace { get; set; }
        //[DataMember(Name = "assemblyName")]
        //public string AssemblyName { get; set; }
        [DataMember(Name = "correlationId")]
        public string CorrelationId { get; set; }
        [DataMember(Name = "routingKey")]
        public string RoutingKey { get; set; }

        //public Type GetMetadataType()
        //{
        //    return TypeMaps.GetOrAdd(string.Concat(this.Namespace, ".", this.TypeName), 
        //        key => Type.GetType(string.Concat(key, ", ", this.AssemblyName), true));
        //}

        //private static readonly ConcurrentDictionary<string, Type> TypeMaps = new ConcurrentDictionary<string, Type>();

        //public string Kind { get; set; }

        /// <summary>
        /// 从入队到出队的时间
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 等待入队的时间
        /// </summary>
        public TimeSpan WaitTime { get; set; }

        /// <summary>
        /// 处理该消息的时长
        /// </summary>
        public TimeSpan ProcessTime { get; set; }


        ///// <summary>
        ///// 元数据
        ///// </summary>
        //[DataContract]
        //[Serializable]
        //public struct Metadata : IEquatable<Metadata>
        //{
        //    public static readonly Metadata Empty = new Metadata();

        //    public Metadata(string str)
        //    {
        //        var match = Regex.Match(str, @"^([\w-\.]+)\.([\w-]+),\s([\w-]+)$");
        //        if (!match.Success) {
        //            throw new FormatException(str);
        //        }

        //        this.Namespace = match.Groups[1].Value;
        //        this.TypeName = match.Groups[2].Value;
        //        this.AssemblyName = match.Groups[3].Value;
        //    }

        //    public Metadata(Type type)
        //        : this(type.Namespace, type.Name,
        //        Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName))
        //    { }

        //    public Metadata(string @namespace, string typeName, string assemblyName)
        //    {
        //        @namespace.NotNullOrWhiteSpace("sourceNamespace");
        //        typeName.NotNullOrWhiteSpace("sourceTypeName");
        //        assemblyName.NotNullOrWhiteSpace("sourceAssemblyName");

        //        this.Namespace = @namespace;
        //        this.TypeName = typeName;
        //        this.AssemblyName = assemblyName;
        //    }

        //    [DataMember(Name = "typeName")]
        //    public string TypeName { get; set; }
        //    [DataMember(Name = "namespace")]
        //    public string Namespace { get; set; }
        //    [DataMember(Name = "assemblyName")]
        //    public string AssemblyName { get; set; }

        //    public override bool Equals(object obj)
        //    {
        //        if (obj == null || obj.GetType() != typeof(Metadata))
        //            return false;

        //        Metadata other = (Metadata)obj;

        //        return IsEqual(this, other);
        //    }

        //    public override int GetHashCode()
        //    {
        //        if (string.IsNullOrEmpty(this.Namespace) && string.IsNullOrEmpty(this.TypeName))
        //            return 0;

        //        return string.Concat(this.Namespace, ".", this.TypeName).GetHashCode();
        //    }

        //    public override string ToString()
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        if (!string.IsNullOrWhiteSpace(this.Namespace))
        //            sb.Append(this.Namespace).Append(".");
        //        if (!string.IsNullOrWhiteSpace(this.TypeName))
        //            sb.Append(this.TypeName);
        //        if (!string.IsNullOrWhiteSpace(this.AssemblyName))
        //            sb.Append(", ").Append(this.AssemblyName);

        //        return sb.ToString();
        //    }

        //    /// <summary>
        //    /// 判断是否相等
        //    /// </summary>
        //    public static bool operator ==(Metadata left, Metadata right)
        //    {
        //        return IsEqual(left, right);
        //    }
        //    /// <summary>
        //    /// 判断是否不相等
        //    /// </summary>
        //    public static bool operator !=(Metadata left, Metadata right)
        //    {
        //        return !IsEqual(left, right);
        //    }

        //    private static bool IsEqual(Metadata left, Metadata right)
        //    {
        //        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null)) {
        //            return false;
        //        }
        //        return ReferenceEquals(left, null) || left.Equals(right);
        //    }

        //    public static Metadata Parse(string input)
        //    {
        //        return new Metadata(input);
        //    }

        //    public static bool TryParse(string input, out Metadata result)
        //    {
        //        if (!Regex.IsMatch(input, @"^([\w-\.]+)\.([\w-]+),\s([\w-]+)$")) {
        //            result = Empty;
        //            return false;
        //        }

        //        result = Parse(input);
        //        return true;
        //    }
        //}
    }
}
