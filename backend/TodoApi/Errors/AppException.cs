namespace TodoApi.Errors;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public IDictionary<string, string[]>? Details { get; }

    public AppException(int statusCode, string code, string message, IDictionary<string, string[]>? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details;
    }

    public static AppException Validation(string message, IDictionary<string, string[]>? details = null)
        => new(400, "validation_failed", message, details);

    public static AppException Unauthorized(string message = "Authentication required.")
        => new(401, "unauthorized", message);

    public static AppException NotFound(string message = "Resource not found.")
        => new(404, "not_found", message);

    public static AppException Conflict(string message)
        => new(409, "conflict", message);
}
