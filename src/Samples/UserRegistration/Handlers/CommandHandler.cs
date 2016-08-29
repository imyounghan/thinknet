using ThinkNet.Messaging.Handling;
using UserRegistration.Commands;


namespace UserRegistration.Handlers
{
    public class CommandHandler :
        ICommandHandler<RegisterUser>
    {
        
        public void Handle(ICommandContext context, RegisterUser command)
        {
            var user = new User(command.LoginId, command.Password, command.UserName, command.Email);
            context.Add(user);
            //Console.WriteLine("添加一个用户{0}", System.Threading.Interlocked.Increment(ref counter));
        }        
    }
}
