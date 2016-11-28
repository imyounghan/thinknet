using System;
using ThinkLib.Annotation;
using ThinkNet.Database;

namespace UserRegistration
{
    [Register(typeof(IUniqueLoginNameService))]
    public class UniqueLoginNameService : IUniqueLoginNameService
    {
        private readonly IDataContextFactory _dataContextFactory;

        public UniqueLoginNameService(IDataContextFactory dataContextFactory)
        {
            this._dataContextFactory = dataContextFactory;
        }

        public bool Validate(string loginName, string correlationId)
        {
            using(var context = _dataContextFactory.Create()) {
                try {
                    var data = context.Find<LoginNameData>(loginName);
                    if(data == null) {
                        context.Save(new LoginNameData() {
                            LoginName = loginName,
                            CorrelationId = correlationId
                        });
                        context.Commit();
                        return true;
                    }
                    return correlationId == data.CorrelationId;
                }
                catch(Exception) {
                    return false;
                }
            }
        }
    }
}
