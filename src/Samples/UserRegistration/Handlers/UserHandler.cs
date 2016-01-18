using System;
using ThinkNet.Kernel;
using ThinkNet.Messaging.Handling;
using UserRegistration.Commands;
using UserRegistration.Events;
using UserRegistration.ReadModel;


namespace UserRegistration.Handlers
{
    public class UserHandler : IMessageHandler<RegisterUser>,
        IMessageHandler<UserCreated>, IMessageHandler<UserLogined>
    {
        private readonly IEventSourcedRepository _repository;
        private readonly IUserDao _userDao;
        public UserHandler(IEventSourcedRepository repository, IUserDao userDao)
        {
            this._repository = repository;
            this._userDao = userDao;
        }

        public void Handle(RegisterUser command)
        {
            Console.ResetColor();
            Console.WriteLine("添加一个新用户");

            var user = new User(command.LoginId, command.Password, command.UserName, command.Email);
            _repository.Save(user, command.Id);
        }

        public void Handle(UserCreated @event)
        {
            //var user = _repository.Get<User>(@event.SourceId);

            (_userDao as UserDao).Save(new UserModel {
                UserID = @event.SourceId,
                LoginId = @event.LoginId,
                Password = @event.Password,
                UserName = @event.UserName
            });

            Console.ResetColor();
            Console.WriteLine("同步到Q端数据库");
        }

        public void Handle(UserLogined @event)
        {
            Console.ResetColor();
            Console.WriteLine("记录登录日志");
        }
    }
}
