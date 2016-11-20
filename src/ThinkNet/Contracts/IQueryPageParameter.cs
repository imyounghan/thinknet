namespace ThinkNet.Contracts
{
    public interface IQueryPageParameter : IQueryParameter
    {
        int PageSize { get; }

        int PageIndex { get; }
    }
}
