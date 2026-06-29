namespace Db4Net;

/// <summary>
/// Represents one page of query results and the total number of matching rows.
/// </summary>
/// <typeparam name="T">The materialized item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Initializes a new paged result.
    /// </summary>
    /// <param name="items">The materialized rows for the requested page.</param>
    /// <param name="totalCount">The total number of rows matching the query filters.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of rows requested per page.</param>
    public PagedResult(IReadOnlyList<T> items, long totalCount, int pageNumber, int pageSize)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count must be greater than or equal to 0.");
        }

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
        }

        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the materialized rows for the requested page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the total number of rows matching the query filters.
    /// </summary>
    public long TotalCount { get; }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of rows requested per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages
    {
        get
        {
            if (TotalCount == 0)
            {
                return 0;
            }

            var totalPages = (TotalCount / PageSize) + (TotalCount % PageSize == 0 ? 0 : 1);
            return totalPages > int.MaxValue ? int.MaxValue : (int)totalPages;
        }
    }
}
