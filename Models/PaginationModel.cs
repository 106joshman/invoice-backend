namespace InvoiceService.Models;

public class PaginationParams
{
    private const int MaxPageSize = 50;

    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}

public class PaginatedResponse<T>
{
    public required List<T> Items { get; set; }

    public int PageNumber { get; set; }

     public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }


    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;
}