namespace Karasu.ERP.Shared.Models;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? code = null) => new(false, error, code);
    public static Result<T> Success<T>(T data) => Result<T>.Success(data);
    public static Result<T> Failure<T>(string error, string? code = null) => Result<T>.Failure(error, code);
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(T? data, bool isSuccess, string? error, string? errorCode)
        : base(isSuccess, error, errorCode)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(data, true, null, null);
    public new static Result<T> Failure(string error, string? code = null) => new(default, false, error, code);
}

public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public PaginatedList(IReadOnlyList<T> items, int count, int page, int pageSize)
    {
        Items = items;
        TotalCount = count;
        Page = page;
        PageSize = pageSize;
    }
}
