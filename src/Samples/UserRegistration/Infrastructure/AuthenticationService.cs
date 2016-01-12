using System;
using System.Linq;
using MemberSample.Application;
using MemberSample.Domain;
using ThinkNet;
using ThinkNet.Components;
using ThinkNet.Infrastructure;
using ThinkNet.Storage;


namespace MemberSample.Infrastructure
{
    [RegisteredComponent(typeof(IAuthenticationService))]
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDataContextFactory _contextFactory;
        public AuthenticationService(IDataContextFactory contextFactory)
        {
            this._contextFactory = contextFactory;
        }


        public bool Authenticate(string loginid, string password)
        {
            Console.WriteLine("调用验证方法");

            using (var context = _contextFactory.CreateDataContext()) {
                var criteria = Criteria<User>.Eval(p => p.LoginId == loginid);
                User user = context.Single(criteria);

                return user != null && user.VertifyPassword(password);
            }
        }
    }
}
