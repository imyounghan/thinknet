using System;
using System.Runtime.Serialization;

namespace ThinkNet.Runtime.Kafka
{
    [DataContract]
    [Serializable]
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
        [DataMember]
        public string AssemblyName { get; set; }
        /// <summary>
        /// 命名空间
        /// </summary>
        [DataMember]
        public string Namespace { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string Metadata { get; set; }

        public Type GetMetadataType()
        {
            string typeFullName = string.Concat(this.Namespace, ".", this.TypeName, ", ", this.AssemblyName);

            return Type.GetType(typeFullName);
        }
    }
}
