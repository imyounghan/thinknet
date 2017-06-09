

namespace ThinkNet.Communication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.ServiceModel;

    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging;

    [ServiceContract(Name = "Request")]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    [DataContractFormat(Style = OperationFormatStyle.Rpc)]
    [ServiceKnownType(typeof(Response))]
    public class WcfRequestService : IInitializer
    {
        private Dictionary<string, Type> commandTypes;
        private Dictionary<string, Type> queryTypes;

        private readonly ICommandService commandService;
        private readonly IQueryService queryService;
        private readonly ITextSerializer serializer;


        //public WcfRequestService()
        //{
        //    this.commandService = ObjectContainer.Instance.Resolve<ICommandService>();
        //    this.queryService = ObjectContainer.Instance.Resolve<IQueryService>();
        //    this.serializer = ObjectContainer.Instance.Resolve<ITextSerializer>();

        //    this.Initialize(ObjectContainer.Instance, new Assembly[]
        //                                                  {
        //                                                      Assembly.Load("UserRegistration")
        //                                                  });
        //}


        public WcfRequestService(ICommandService commandService, IQueryService queryService, ITextSerializer serializer)
        {
            this.commandService = commandService;
            this.queryService = queryService;
            this.serializer = serializer;
        }

        [OperationContract]
        public IResponse Send(string typeName, string data)
        {
            Type type;
            if (!commandTypes.TryGetValue(typeName, out type))
            {
                try
                {
                    type = Type.GetType(typeName);
                }
                catch (Exception)
                {
                    return new Response { ErrorCode = "-1", ErrorMessage = "Unkown Type.", Status = (int)ExecutionStatus.Failed };
                }
            }

            var command = (ICommand)serializer.Deserialize(data, type);
            var commandResult = commandService.Send(command);

            return new Response {
                ErrorCode = commandResult.ErrorCode,
                ErrorMessage = commandResult.ErrorMessage,
                Status = (int)commandResult.Status,
            };
            
        }

        [OperationContract]
        public IResponse Execute(string typeName, string data)
        {
            Type type;
            Response response;
            if(commandTypes.TryGetValue(typeName, out type)) {
                var command = (ICommand)serializer.Deserialize(data, type);
                var commandResult = commandService.Execute(command);

                response = new Response {
                    ErrorCode = commandResult.ErrorCode,
                    ErrorMessage = commandResult.ErrorMessage,
                    Status = (int)commandResult.Status,
                };

                if (commandResult.ErrorData != null && commandResult.ErrorData.Count > 0)
                {
                    response.Result = serializer.Serialize(commandResult.ErrorData);
                }

                return response;
            }

            if(queryTypes.TryGetValue(typeName, out type)) {
                var query = (IQuery)serializer.Deserialize(data, type);
                var queryResult = queryService.Execute(query);
                response = new Response {
                    ErrorMessage = queryResult.ErrorMessage,
                    Status = (int)queryResult.Status,
                };

                if (queryResult.Status == ExecutionStatus.Success 
                    && queryResult.Data != null
                    && queryResult.Data != DBNull.Value)
                {
                    response.Result = serializer.Serialize(queryResult.Data);
                    response.ResultType = queryResult.Data.GetType().GetFullName();
                }

                return response;
            }

            return new Response { ErrorCode = "-1", ErrorMessage = "Unkown Type.", Status = (int)ExecutionStatus.Failed };
        }

        #region IInitializer 成员

        public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            Type commandType = typeof(ICommand);
            Type queryType = typeof(IQuery);
            Type baseType = typeof(IMessage);

            Type[] messageTypes =
                assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                    .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                    .ToArray();

            commandTypes = messageTypes.Where(type => commandType.IsAssignableFrom(type))
                .ToDictionary(type => type.Name, type => type);
            queryTypes = messageTypes.Where(type => queryType.IsAssignableFrom(type))
                .ToDictionary(type => type.Name, type => type);
        }

        #endregion
    }
}
