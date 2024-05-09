using System;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Todo.Web.Server.Extensions
{
    public static class RateLimitExtensions
    {
        private const string Policy = "PerUserRatelimit";

        public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            return services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(Policy, context =>
                {
                    // We always have a user name
                    var username = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                    return RateLimitPartition.GetTokenBucketLimiter(username, key => new()
                    {
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        AutoReplenishment = true,
                        TokenLimit = 100,
                        TokensPerPeriod = 100,
                        QueueLimit = 100,
                    });
                });
            });
        }

        public static IEndpointConventionBuilder RequirePerUserRateLimit(this IEndpointConventionBuilder builder)
        {
            return builder.RequireRateLimiting(Policy);
        }
    }
}
