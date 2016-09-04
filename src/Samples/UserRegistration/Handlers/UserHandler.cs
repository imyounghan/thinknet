using System;
using ThinkNet.Messaging.Handling;
using UserRegistration.Commands;
using UserRegistration.Events;
using UserRegistration.ReadModel;


namespace UserRegistration.Handlers
{
    public class UserHandler :
        ICommandHandler<RegisterUser>,
        IEventHandler<UserCreated>
    {
        private readonly IUniqueLoginNameService _uniqueService;
        private readonly IUserDao _userDao;
        public UserHandler(IUniqueLoginNameService uniqueService, IUserDao userDao)
        {
            this._userDao = userDao;
            this._uniqueService = uniqueService;
        }

        public void Handle(ICommandContext context, RegisterUser command)
        {
            var user = new UserRegisterService(_uniqueService, command.Id)
                .Register(command.LoginId, command.Password, command.UserName, command.Email);
            context.Add(user);
        }


        public void Handle(int version, UserCreated @event)
        {
            _userDao.Save(new UserModel {
                UserID = @event.SourceId,
                LoginId = @event.LoginId,
                Password = @event.Password,
                UserName = @event.UserName
            });

            Console.WriteLine("同步到Q端数据库");
        }
    }
}
