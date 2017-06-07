using System;
using System.Collections.Concurrent;
using ThinkNet;

namespace UserRegistration
{

    [Register(typeof(IUniqueLoginNameService))]
    public class UniqueLoginNameService : IUniqueLoginNameService
    {
        private readonly ConcurrentDictionary<string, string> dict = new ConcurrentDictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        //private readonly IDataContextFactory _dataContextFactory;

        //public UniqueLoginNameService(IDataContextFactory dataContextFactory)
        //{
        //    this._dataContextFactory = dataContextFactory;
        //}


        public bool Validate(string loginName, string correlationId)
        {
            string commandId;
            if(!dict.TryGetValue(loginName, out commandId)) {
                return dict.TryAdd(loginName, correlationId);
            }
            
            return correlationId == commandId;
            //using(var context = _dataContextFactory.Create()) {
            //    try {
            //        var data = context.Find<LoginNameData>(loginName);
            //        if(data == null) {
            //            context.Save(new LoginNameData() {
            //                LoginName = loginName,
            //                CorrelationId = correlationId
            //            });
            //            context.Commit();
            //            return true;
            //        }
            //        return correlationId == data.CorrelationId;
            //    }
            //    catch(Exception) {
            //        return false;
            //    }
            //}
        }
    }
}
