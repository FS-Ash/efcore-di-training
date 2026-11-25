using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingPlan.Exceptions;

namespace TrainingPlan.Handlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        public GlobalExceptionHandler(
            IProblemDetailsService problemDetailsService,
            ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, title, detail) = exception switch
            {
                OrderNotFoundException => (
                    StatusCodes.Status404NotFound,
                    "Order not found",
                    exception.Message),

                InvalidOrderStateException => (
                    StatusCodes.Status409Conflict,
                    "Invalid order state",
                    exception.Message),

                DbUpdateConcurrencyException => (
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The order was modified by another operation."),

                MembershipNotFoundException => (
                    StatusCodes.Status404NotFound,
                    "Membership not found",
                    exception.Message),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Internal server error",
                    "An unexpected error occurred.")
            };

            if (statusCode >= 500)
            {
                _logger.LogError(
                    exception,
                    "Unhandled request failure. Path: {RequestPath}",
                    httpContext.Request.Path);
            }

            httpContext.Response.StatusCode = statusCode;

            return await _problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Detail = detail,
                        Instance = httpContext.Request.Path.Value
                    }
                });
        }
    }
}