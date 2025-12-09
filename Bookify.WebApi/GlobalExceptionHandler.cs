using Bookify.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.WebApi;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        var (statusCode, title) = MapException(exception);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Type = exception.GetType().Name,
            Title = title,
            Detail = exception.Message
        };
        
        switch (exception)
        {
            case ValidationException validationException:
                problemDetails.Extensions["errors"] = validationException.Errors; 
                break;
            
            case { } when statusCode == StatusCodes.Status500InternalServerError:
                // СКРЫВАЕМ детали для 500-х ошибок в продакшене
                problemDetails.Detail = "An internal server error has occurred.";
                break;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static (int StatusCode, string Title) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Failure"),
            ConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };
    }
}