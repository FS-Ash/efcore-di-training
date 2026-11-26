namespace TrainingPlan.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[HeaderName]
                .ToString();

            if (string.IsNullOrWhiteSpace(correlationId) ||
                correlationId.Length > 128 ||
                !correlationId.All(c =>
                    char.IsAsciiLetterOrDigit(c) ||
                    c == '-' || c == '_' || c == '.'))
            {
                correlationId = context.TraceIdentifier;
            }

            context.TraceIdentifier = correlationId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;

                return Task.CompletedTask;
            });

            using var scope = _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId
                });

            await _next(context);
        }
    }
}