using System.Net;
using System.Text.Json;
using FlemanApi.Exceptions;

namespace FlemanApi.Middleware;

// Mirrors com.fleman.exception.GlobalExceptionHandler (@RestControllerAdvice) —
// requirement #4. Every unhandled exception, anywhere in the pipeline, comes
// out the wire as the same { message, status, timestamp } JSON shape.
// FluentValidation failures are handled separately by the ApiBehaviorOptions
// .InvalidModelStateResponseFactory registered in Program.cs (Java's
// MethodArgumentNotValidException equivalent), so this middleware only needs
// to cover ApiException and everything else.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "API exception: {Message}", ex.Message);
            await WriteErrorAsync(context, (int)ex.Status, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, (int)HttpStatusCode.InternalServerError, "Unexpected error occurred");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;
        var body = new ErrorResponse(message, status);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
