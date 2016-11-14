
namespace ThinkNet.ReadData
{
    public interface IQueryExecutor<TParameter>
        where TParameter : class, IQueryParameter
    {
        IQueryResult Execute(TParameter parameter);
    }
}
