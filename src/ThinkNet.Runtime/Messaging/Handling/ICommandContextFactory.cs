
namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示创建命令上下文的工厂接口
    /// </summary>
    public interface ICommandContextFactory
    {
        ICommandContext CreateCommandContext();
    }
}
