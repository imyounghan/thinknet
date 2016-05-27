using System;
using ThinkNet.Kernel;
using ThinkNet.Messaging.Handling;
using UserRegistration.Commands;
using UserRegistration.Events;
using UserRegistration.ReadModel;


namespace UserRegistration.Handlers
{
    public class UserHandler : 
        IHandler<RegisterUser>,
        ICommandHandler<RegisterUser>,
        IEventHandler<UserCreated>,
        IHandler<UserLogined>
    {
        private readonly IEventSourcedRepository _repository;
        private readonly IUserDao _dao;
        public UserHandler(IEventSourcedRepository repository, IUserDao dao)
        {
            this._repository = repository;
            this._dao = dao;
        }

        public void Handle(RegisterUser command)
        {
            var user = new User(command.LoginId, command.Password, command.UserName, command.Email);
            _repository.Save(user, command.Id);

            //Console.WriteLine("{0}, {1}", DateTime.UtcNow, "添加一个新用户");
        }

        public void Handle(IEventContext context, UserCreated @event)
        {
            _dao.Save(new UserModel {
                UserID = @event.SourceId,
                LoginId = @event.LoginId,
                Password = @event.Password,
                UserName = @event.UserName
            });

            //Console.WriteLine("同步到Q端数据库");
        }

        public void Handle(UserLogined @event)
        {
            Console.WriteLine("记录登录日志");
        }


        #region ICommandHandler<RegisterUser> 成员

        public void Handle(ICommandContext context, RegisterUser command)
        {
            this.Handle(command);
        }

        #endregion
    }
}
