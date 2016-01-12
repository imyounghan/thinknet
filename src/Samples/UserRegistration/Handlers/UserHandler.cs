using System;
using ThinkNet.Kernel;
using ThinkNet.Messaging.Handling;
using UserRegistration.Commands;
using UserRegistration.Events;


namespace UserRegistration.Handlers
{
    public class UserHandler : IMessageHandler<RegisterUser>,
        IMessageHandler<UserCreated>, IMessageHandler<UserLogined>
    {
        private readonly IEventSourcedRepository _repository;
        public UserHandler(IEventSourcedRepository repository)
        {
            this._repository = repository;
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

            //using (var context = _contextFactory.CreateRepositoryContext()) {
            //    context.GetRepository<IRepository<User>>().Add(user);
            //    context.Commit();
            //}

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
