using System.Net;
using System.Text.Json;
using InvoiceService.DTOs;
using Microsoft.EntityFrameworkCore;
using Npgsql;


namespace InvoiceService.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
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
                // ✅ UNIQUE CONSTRAINT VIOLATION HANDLING
                case DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505":
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Error = "DuplicateEntry";
                     if (pgEx.ConstraintName?.Contains("Customers_Email") == true)
                        errorResponse.Message = "A customer with this email already exists.";
                    else if (pgEx.ConstraintName?.Contains("Customers_BusinessName") == true)
                        errorResponse.Message = "A customer with this business name already exists.";
                    else
                        errorResponse.Message = "Duplicate record detected.";
                    break;

                case DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23503":
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Error = "ForeignKeyConstraint";
                    errorResponse.Message = "Cannot delete record because it has related data.";
                    break;

                // UNAUTHORIZED (401)
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Error = "Unauthorized";
                    errorResponse.Message = ex.Message;
                    break;

                // NOT FOUND (404)
                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Error = "NotFound";
                    errorResponse.Message = ex.Message;
                    break;

                // BAD REQUEST (400)
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
