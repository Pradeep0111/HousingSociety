using System;
using System.Threading.Tasks;
using Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware
{
    // Central place that turns Application-layer exceptions into ProblemDetails
    // responses, so controllers stay thin and never construct error bodies themselves.
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception exception)
            {
                var (statusCode, title, detail) = exception switch
                {
                    AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
                    NotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
                    ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
                    InvalidTransitionException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
                    ValidationException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
                    _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred."),
                };

                httpContext.Response.StatusCode = statusCode;
                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail,
                    Instance = httpContext.Request.Path,
                });
            }
        }
    }
}
