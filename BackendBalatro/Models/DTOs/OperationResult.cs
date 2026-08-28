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

    public static implicit operator bool(OperationResult result) => result.Success;
    public static implicit operator OperationResult((bool Success, string Message) tuple) => new(tuple.Success, tuple.Message);
    public static implicit operator (bool Success, string Message)(OperationResult result) => (result.Success, result.Message);
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

    public static implicit operator OperationResult<T>((bool Success, string Message, T? Data) tuple) => new(tuple.Success, tuple.Message, tuple.Data);
    public static implicit operator (bool Success, string Message, T? Data)(OperationResult<T> result) => (result.Success, result.Message, result.Data);
}
