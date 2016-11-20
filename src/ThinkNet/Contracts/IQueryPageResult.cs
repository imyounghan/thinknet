namespace ThinkNet.Contracts
{
    /// <summary>
    /// 查询分页数据结果
    /// </summary>
    public interface IQueryPageResult<T> : IQueryMultipleResult<T>
    {
        /// <summary>
        /// 获取或设置总记录数。
        /// </summary>
        int TotalRecords { get; }

        /// <summary>
        /// 获取或设置页数。
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// 获取或设置页面大小。
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// 获取或设置页码。
        /// </summary>
        int PageIndex { get; }
    }
}
