using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.Domain.Exceptions;

namespace SkyRoute.Api.Middleware;

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
        catch (SearchExpiredException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status410Gone, "Search expired", ex.Message);
        }
        catch (FlightNotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Flight not found", ex.Message);
        }
        catch (ValidationException ex)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
            };

            problem.Extensions["errors"] = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            await WriteProblemAsync(context, problem);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            // Deliberately don't leak the real exception message to the response body.
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", detail: null);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string? detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
        };

        return WriteProblemAsync(context, problem);
    }

    private static Task WriteProblemAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(problem);
    }
}
