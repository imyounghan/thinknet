
namespace ThinkLib.Contexts
{
    /// <summary>
    /// 实体验证接口
    /// </summary>
    public interface IValidatable<TContext>
    {
        /// <summary>
        /// 持久化之前验证
        /// </summary>
        void Validate(TContext context);
    }
}
