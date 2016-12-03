using System.ComponentModel.Composition;
using System.ServiceModel;
using System.Threading.Tasks;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using UserRegistration.Commands;

namespace UserRegistration
{
    [ServiceContract(Name = "CommandService", Namespace = "http://www.thinknet.com")]
    [DataContractFormat(Style = OperationFormatStyle.Rpc)]
    [ServiceKnownType(typeof(RegisterUser))]
    [ServiceKnownType(typeof(CommandResult))]
    public interface ICommandClient
    {
        /// <summary>
        /// 发送命令
        /// </summary>
        [OperationContract]
        void Send(ICommand command);

        /// <summary>
        /// 执行命令
        /// </summary>
        [OperationContract]
        ICommandResult Execute(ICommand command, CommandReturnType returnType);
    }

    [Export(typeof(ICommandService))]
    public class CommandService : ICommandService
    {

        private readonly ChannelFactory<ICommandClient> channelFactory;

        public CommandService()
        {
            channelFactory = new ChannelFactory<ICommandClient>(new NetTcpBinding(), "net.tcp://127.0.0.1:9999/CommandService");
        }

        public ICommandResult Execute(ICommand command, CommandReturnType returnType)
        {
            return channelFactory.CreateChannel().Execute(command, returnType);
        }

        public Task<ICommandResult> ExecuteAsync(ICommand command, CommandReturnType returnType)
        {
            return Task.Factory.StartNew(() => this.Execute(command, returnType));
        }

        public void Send(ICommand command)
        {
            channelFactory.CreateChannel().Send(command);
        }

        public Task SendAsync(ICommand command)
        {
            return Task.Factory.StartNew(() => this.Send(command));
        }
    }
}
