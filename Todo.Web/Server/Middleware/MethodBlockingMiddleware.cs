using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Todo.Web.Server.Middleware
{
    public class MethodBlockingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MethodBlockingMiddleware> _logger;
        private readonly IDictionary<string, HashSet<string>> _routeMethodRestrictions = new Dictionary<string, HashSet<string>>
        {
            {"/authentication/register", new HashSet<string> { "POST" } }
        };

        public MethodBlockingMiddleware(RequestDelegate next, ILogger<MethodBlockingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestPath = context.Request.Path.Value.ToLowerInvariant();
            var requestMethod = context.Request.Method;

            _logger.LogInformation($"Processing request {requestMethod} {requestPath}");

            if (_routeMethodRestrictions.TryGetValue(requestPath, out var allowedMethods))
            {
                if (!allowedMethods.Contains(requestMethod))
                {
                    _logger.LogWarning($"Method {requestMethod} not allowed for path {requestPath}");

                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    context.Response.Headers["Allow"] = string.Join(", ", allowedMethods);

                    await context.Response.WriteAsync("Method Not Allowed");

                    return;
                }
            }

            await _next(context);
        }
    }
}