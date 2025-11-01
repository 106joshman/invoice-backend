using System.Net;
using System.Text.Json;
using InvoiceService.DTOs;

namespace InvoiceService.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // continue pipeline
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Error = "ServerError",
                Message = "An unexpected error occurred.",
                Details = ex.Message // only for debugging, can hide in prod
            };

            // Map exception types
            switch (ex)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Error = "Unauthorized";
                    errorResponse.Message = ex.Message;
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Error = "NotFound";
                    errorResponse.Message = ex.Message;
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Error = "BadRequest";
                    errorResponse.Message = ex.Message;
                    break;
            }

            var result = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(result);
        }
    }
}
