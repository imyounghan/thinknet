using System;
using ThinkNet.Database;
using ThinkNet.Kernel;
using ThinkNet.Messaging.Handling;
using UserRegistration.Commands;
using UserRegistration.Events;
using UserRegistration.ReadModel;


namespace UserRegistration.Handlers
{
    public class UserHandler : 
        IHandler<RegisterUser>,
        IHandler<UserCreated>, 
        IHandler<UserLogined>
    {
        private readonly IEventSourcedRepository _repository;
        private readonly IDataContextFactory _contextFactory;
        public UserHandler(IEventSourcedRepository repository, IDataContextFactory contextFactory)
        {
            this._repository = repository;
            this._contextFactory = contextFactory;
        }

        public void Handle(RegisterUser command)
        {
            var user = new User(command.LoginId, command.Password, command.UserName, command.Email);
            _repository.Save(user, command.Id);

            Console.WriteLine("添加一个新用户");
        }

        public void Handle(UserCreated @event)
        {
            using (var context = _contextFactory.CreateDataContext()) {
                context.Save(new UserModel {
                    UserID = @event.SourceId,
                    LoginId = @event.LoginId,
                    Password = @event.Password,
                    UserName = @event.UserName
                });
                context.Commit();
            }

            Console.WriteLine("同步到Q端数据库");
        }

        public void Handle(UserLogined @event)
        {
            Console.WriteLine("记录登录日志");
        }
    }
}
