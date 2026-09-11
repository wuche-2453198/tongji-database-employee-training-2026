namespace TrainingManagement.Api.Common.Responses;

/// <summary>分页响应，承载当前页记录和分页统计。</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long Total { get; init; }

    /// <summary>创建空分页对象，供对象初始化器或反序列化使用。</summary>
    public PagedResult()
    {
    }

    /// <summary>使用查询结果、页码、每页数量和总记录数构造分页响应。</summary>
    public PagedResult(IReadOnlyCollection<T> items, int page, int pageSize, long total)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        Total = total;
    }
}
