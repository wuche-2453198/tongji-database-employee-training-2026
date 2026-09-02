namespace TrainingManagement.Api.Common;
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "ok";
    public T? Data { get; set; }
    public string TraceId { get; set; } = Guid.NewGuid().ToString();

    public static ApiResponse<T> Success(T data, string message = "ok") =>
        new ApiResponse<T> { Success = true, Message = message, Data = data };
    public static ApiResponse<T> Fail(string message) =>
        new ApiResponse<T> { Success = false, Message = message };
}
