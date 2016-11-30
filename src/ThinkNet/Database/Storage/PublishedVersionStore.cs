using System.Linq;
using System.Threading.Tasks;
using ThinkLib;
using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Database.Storage
{
    public class PublishedVersionStore : PublishedVersionInMemory
    {
        private readonly IDataContextFactory _dataContextFactory;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public PublishedVersionStore(IDataContextFactory dataContextFactory)
        {
            this._dataContextFactory = dataContextFactory;
        }

        public override int GetPublishedVersion(DataKey sourceKey)
        {
            return Task.Factory.StartNew(()=> {
                using(var context = _dataContextFactory.Create()) {                    
                    var queryable = context.CreateQuery<EventData>();

                    if(queryable.IsEmpty())
                        return 0;

                    queryable = queryable.Where(p => p.AggregateRootId == sourceKey.Id &&
                        p.AggregateRootTypeCode == sourceKey.GetSourceTypeName().GetHashCode());
                    return !queryable.Any() ? 0 : queryable.Max(p => p.Version);
                }
            }).Result;
        }
    }
}
