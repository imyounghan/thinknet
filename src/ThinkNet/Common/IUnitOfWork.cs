
namespace ThinkNet.Common
{
    /// <summary>
    /// 表示继承该接口的类型是一个工作单元
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// 提交
        /// </summary>
        void Commit();
    }
}
