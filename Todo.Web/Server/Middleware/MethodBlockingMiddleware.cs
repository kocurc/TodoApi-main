using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Todo.Web.Server.Middleware
{
    public class MethodBlockingMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;
        private readonly IDictionary<string, HashSet<string>> _routeMethodRestrictions = new Dictionary<string, HashSet<string>>
        {
            {"/authentication/register", ["POST"] },
        };

        public async Task Invoke(HttpContext context)
        {
            var requestPath = context.Request.Path.Value.ToLowerInvariant();
            var requestMethod = context.Request.Method;

            if (_routeMethodRestrictions.TryGetValue(requestPath, out var allowedMethods))
            {
                if (!allowedMethods.Contains(requestMethod))
                {
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
