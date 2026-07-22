namespace TrainingManagement.Api.Common.Responses;

public sealed class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long Total { get; init; }

    public PagedResult()
    {
    }

    public PagedResult(IReadOnlyCollection<T> items, int page, int pageSize, long total)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        Total = total;
    }
}
