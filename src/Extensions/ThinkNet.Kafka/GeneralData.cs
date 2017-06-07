using System;
using System.Runtime.Serialization;
using System.Text;

namespace ThinkNet.Runtime.Kafka
{
    [DataContract]
    public class GeneralData
    {
        public GeneralData()
        { }

        public GeneralData(Type sourceType)
                : this(sourceType.Namespace,
                sourceType.Name,
                sourceType.GetAssemblyName())
            { }

        public GeneralData(string sourceTypeName)
                : this(string.Empty, sourceTypeName, string.Empty)
            { }
        public GeneralData(string sourceNamespace, string sourceTypeName)
                : this(sourceNamespace, sourceTypeName, string.Empty)
            { }

        public GeneralData(string sourceNamespace, string sourceTypeName, string sourceAssemblyName)
        {
            this.Namespace = sourceNamespace;
            this.TypeName = sourceTypeName;
            this.AssemblyName = sourceAssemblyName;
        }

        /// <summary>
        /// 程序集
        /// </summary>
        [DataMember(Name = "assemblyName")]
        public string AssemblyName { get; set; }
        /// <summary>
        /// 命名空间
        /// </summary>
        [DataMember(Name = "namespace")]
        public string Namespace { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        [DataMember(Name = "typeName")]
        public string TypeName { get; set; }

        [DataMember(Name = "metadata")]
        public string Metadata { get; set; }

        public string GetMetadataTypeName()
        {
            StringBuilder sb = new StringBuilder();
            if(!string.IsNullOrWhiteSpace(this.Namespace))
                sb.Append(this.Namespace).Append(".");
            if(!string.IsNullOrWhiteSpace(this.TypeName))
                sb.Append(this.TypeName);
            if(!string.IsNullOrWhiteSpace(this.AssemblyName))
                sb.Append(", ").Append(this.AssemblyName);

            return sb.ToString();

            //string typeFullName = string.Concat(this.Namespace, ".", this.TypeName, ", ", this.AssemblyName);

            //return Type.GetType(typeFullName);
        }
    }
}
