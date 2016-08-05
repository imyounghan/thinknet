
namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示继承该接口的是一个工作单元
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// 提交所有更改
        /// </summary>
        void Commit();
        /// <summary>
        /// 回滚所有操作
        /// </summary>
        void Rollback();
    }
}
