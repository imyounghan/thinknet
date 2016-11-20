using System.Collections.Generic;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 查询返回多个值的结果
    /// </summary>
    public interface IQueryMultipleResult<T> : IQueryResult, IEnumerable<T>
    { }
}
