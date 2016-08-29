using System;
using ThinkNet.Messaging.Handling;
using UserRegistration.Events;
using UserRegistration.ReadModel;

namespace UserRegistration.Handlers
{
    public class EventHandler : 
        IEventHandler<UserCreated>,
        IMessageHandler<UserLogined>
    {
        private readonly IUserDao _dao;
        public EventHandler(IUserDao dao)
        {
            this._dao = dao;
        }

        public void Handle(UserLogined @event)
        {
            Console.WriteLine("记录登录日志");
        }

        public void Handle(int version, UserCreated @event)
        {
            _dao.Save(new UserModel {
                UserID = @event.SourceId,
                LoginId = @event.LoginId,
                Password = @event.Password,
                UserName = @event.UserName
            });

            Console.WriteLine("同步到Q端数据库");
        }
    }
}
