
namespace ThinkNet.Database.Context
{
    /// <summary>
    /// 对象生命周期调用
    /// </summary>
    public interface ILifecycle<TContext>
    {
        /// <summary>
        /// Insert前回调
        /// </summary>
        LifecycleVeto OnSaving(TContext context);
        /// <summary>
        /// Update前回调
        /// </summary>
        LifecycleVeto OnUpdating(TContext context);
        /// <summary>
        /// Delete前回调
        /// </summary>
        LifecycleVeto OnDeleting(TContext context);
        /// <summary>
        /// Load后回调
        /// </summary>
        void OnLoaded(TContext context);
    }
}
