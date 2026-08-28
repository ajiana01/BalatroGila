namespace BackendBalatro.Models.DTOs;

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public OperationResult() { }

    public OperationResult(bool success, string message = "")
    {
        Success = success;
        Message = message;
    }

    public static OperationResult Ok(string message = "Success") => new(true, message);
    public static OperationResult Fail(string message) => new(false, message);

    public void Deconstruct(out bool success, out string message)
    {
        success = Success;
        message = Message;
    }
}

public class OperationResult<T> : OperationResult
{
    public T? Data { get; set; }
    public T? Result => Data;
    public T? Value => Data;

    public OperationResult() { }

    public OperationResult(bool success, string message = "", T? data = default) : base(success, message)
    {
        Data = data;
    }

    public static OperationResult<T> Ok(T data, string message = "Success") => new(true, message, data);
    public new static OperationResult<T> Fail(string message) => new(false, message, default);

    public void Deconstruct(out bool success, out string message, out T? data)
    {
        success = Success;
        message = Message;
        data = Data;
    }
}
