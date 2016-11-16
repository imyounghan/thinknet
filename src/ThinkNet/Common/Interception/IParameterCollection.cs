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
        /// 通过参数名称获取该参数的值
        /// </summary>
        object this[string parameterName] { get; }
        ///// <summary>
        ///// 通过参数位置获取该参数的值
        ///// </summary>
        //object this[int index] { get; }

        /// <summary>
        /// 检查是否包含该参数名称。
        /// </summary>
        bool ContainsParameter(string parameterName);

        ParameterInfo GetParameterInfo(int index);

        ParameterInfo GetParameterInfo(string parameterName);
    }
}
