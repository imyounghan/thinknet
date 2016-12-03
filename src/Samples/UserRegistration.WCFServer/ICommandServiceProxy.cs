using System.ServiceModel;
using ThinkNet.Contracts;
using UserRegistration.Commands;

namespace UserRegistration.ApplicationService
{
    [ServiceContract(Name = "CommandService", Namespace = "http://www.thinknet.com")]
    [DataContractFormat(Style = OperationFormatStyle.Rpc)]
    [ServiceKnownType(typeof(RegisterUser))]
    public interface ICommandServiceProxy
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
}
