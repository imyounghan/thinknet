namespace ThinkNet.Contracts
{
    //public class SingleQueryResult<T> : QueryResult
    //{
    //    public SingleQueryResult(T data)
    //    {
    //        this.Data = data;
    //        this.Status = QueryStatus.Success;
    //    }

    //    T Data { get; set; }
    //}

    /// <summary>
    /// 查询返回单个值的结果
    /// </summary>
    public interface IQuerySingleResult<T> : IQueryResult
    {
        T Result { get; }
    }
}
