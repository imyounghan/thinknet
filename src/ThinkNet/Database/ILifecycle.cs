
namespace ThinkNet.Database
{
    /// <summary>
    /// 对象生命周期调用
    /// </summary>
    public interface ILifecycle
    {
        /// <summary>
        /// Insert前回调
        /// </summary>
        LifecycleVeto OnSaving(IDataContext context);
        /// <summary>
        /// Update前回调
        /// </summary>
        LifecycleVeto OnUpdating(IDataContext context);
        /// <summary>
        /// Delete前回调
        /// </summary>
        LifecycleVeto OnDeleting(IDataContext context);
        /// <summary>
        /// Load后回调
        /// </summary>
        void OnLoaded(IDataContext context);
    }
}
