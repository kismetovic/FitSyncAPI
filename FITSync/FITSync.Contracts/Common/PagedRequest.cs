using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Common;

/// <summary>
/// Base for every list endpoint. PageSize is hard-capped so a client cannot ask the
/// API to materialise the whole table.
/// </summary>
public class PagedRequest
{
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 20;

    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;
}
