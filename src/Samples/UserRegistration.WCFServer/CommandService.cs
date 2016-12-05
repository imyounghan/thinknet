using System.ServiceModel;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using UserRegistration.Commands;

namespace UserRegistration
{
    [ServiceContract(Name = "CommandService", Namespace = "http://www.thinknet.com")]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    [DataContractFormat(Style = OperationFormatStyle.Rpc)]
    [ServiceKnownType(typeof(RegisterUser))]
    [ServiceKnownType(typeof(CommandResult))]
    public class CommandService
    {
        private readonly ICommandService realService;

        public CommandService()
        {
            realService = ObjectContainer.Instance.Resolve<ICommandService>();
        }

        
        [OperationContract]
        public void Send(ICommand command)
        {
            realService.SendAsync(command).Wait(2000);
        }
        [OperationContract]
        public ICommandResult Execute(ICommand command, CommandReturnType returnType)
        {
            return realService.Execute(command, returnType);
        }        
    }
}
