
namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示继承该接口的是一个查询参数
    /// </summary>
    public interface IQueryParameter : IDataTransferObject
    {
        /// <summary>
        /// 查询参数的标识
        /// </summary>
        string Id { get; }
    }
}
