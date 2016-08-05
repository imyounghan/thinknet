using System;
using ThinkNet.Messaging.Handling;
using UserRegistration.Commands;
using UserRegistration.Events;
using UserRegistration.ReadModel;


namespace UserRegistration.Handlers
{
    public class UserHandler :
        ICommandHandler<RegisterUser>,
        IEventHandler<UserCreated>,
        IHandler<UserLogined>
    {
        private readonly IUserDao _dao;
        //private readonly IEventSourcedRepository _repository;
        public UserHandler(/*IEventSourcedRepository repository, */IUserDao dao)
        {
            //this._repository = repository;
            this._dao = dao;
        }

        public void Handle(UserLogined @event)
        {
            Console.WriteLine("记录登录日志");
        }




        #region ICommandHandler<RegisterUser> 成员

        public void Handle(ICommandContext context, RegisterUser command)
        {
            var user = new User(command.LoginId, command.Password, command.UserName, command.Email);
            context.Add(user);
            //Console.WriteLine("添加一个用户{0}", System.Threading.Interlocked.Increment(ref counter));
        }

        #endregion

        #region IEventHandler<UserCreated> 成员

        public void Handle(IEventContext context, UserCreated @event)
        {
            _dao.Save(new UserModel {
                UserID = @event.SourceId,
                LoginId = @event.LoginId,
                Password = @event.Password,
                UserName = @event.UserName
            });

            Console.WriteLine("同步到Q端数据库");
        }

        #endregion
    }
}
