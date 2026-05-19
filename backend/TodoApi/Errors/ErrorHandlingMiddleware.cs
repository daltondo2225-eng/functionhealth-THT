using System.Text.Json;

namespace TodoApi.Errors;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteError(context, ex.StatusCode, ex.Code, ex.Message, ex.Details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteError(context, 500, "server_error", "An unexpected error occurred.");
        }
    }

    public static async Task WriteError(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        IDictionary<string, string[]>? details = null)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            error = new
            {
                code,
                message,
                details = details is { Count: > 0 } ? details : null
            }
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, payload, JsonOptions);
    }
}
