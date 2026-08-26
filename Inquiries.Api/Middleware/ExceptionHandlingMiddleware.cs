using Inquiries.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inquiries.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            var (statusCode, title, detail) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", ex.Message),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.")
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
