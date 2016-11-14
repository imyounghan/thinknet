using System.Collections;
using System.Reflection;

namespace ThinkNet.Common.Interception
{
    /// <summary>
    /// 表示参数集合。
    /// </summary>
    public interface IParameterCollection : ICollection
    {
        /// <summary>
        /// 获取该参数的值
        /// </summary>
        object this[string parameterName] { get; }

        /// <summary>
        /// 检查是否包含该参数名称。
        /// </summary>
        bool ContainsParameter(string parameterName);

        ParameterInfo GetParameterInfo(int index);

        ParameterInfo GetParameterInfo(string parameterName);
    }
}
