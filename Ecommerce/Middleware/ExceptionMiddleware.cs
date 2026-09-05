using Application.Errors;
using System.Net;
using System.Text.Json;

namespace Ecommerce.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            // معالجة أخطاء الـ Domain والـ State Machine وإرجاع 400 Bad Request
            _logger.LogWarning(ex, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ApiResponse((int)HttpStatusCode.BadRequest, ex.Message);

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
        catch (Exception ex)
        {
            // معالجة أخطاء السيرفر غير المتوقعة وإرجاع 500 Internal Server Error
            _logger.LogError(ex, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // الوصول لأول InnerException للحصول على السبب الرئيسي
            var root = ex;
            while (root.InnerException != null)
            {
                root = root.InnerException;
            }

            var detailedMessage = $"[{root.GetType().Name}] {root.Message}";

            var response = _env.IsDevelopment()
                ? new ApiException((int)HttpStatusCode.InternalServerError, detailedMessage, root.StackTrace?.ToString())
                : new ApiException((int)HttpStatusCode.InternalServerError);

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}