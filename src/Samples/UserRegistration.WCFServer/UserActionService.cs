using System.ServiceModel;
using ThinkNet;
using ThinkNet.Messaging;
using UserRegistration.Commands;
using UserRegistration.Contracts;

namespace UserRegistration.Application
{
    [Register(typeof(IUserActionService))]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class UserActionService : IUserActionService
    {
        private readonly ICommandService _commandService;
        public UserActionService(ICommandService commandService)
        {
            this._commandService = commandService;
        }

        #region IUserActionService 成员

        public void RegisterUser(UserInfo user)
        {
            var registerUser = new RegisterUser() {
                LoginId = user.LoginId,
                Password = user.Password,
                UserName = user.UserName,
                Email = user.Email
            };
            _commandService.Execute(registerUser, CommandReturnType.DomainEventHandled);
        }

        #endregion
    }
}
