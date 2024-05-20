using Microsoft.AspNetCore.Builder;

namespace Todo.Web.Server.Extensions;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSecurityHeadersPolicies(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseSecurityHeaders(policies => policies
            .AddFrameOptionsDeny()
            .AddXssProtectionBlock()
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains(
                maxAgeInSeconds: 60 * 60 * 24 * 365)
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .RemoveServerHeader()
            .AddContentSecurityPolicy(builder =>
            {
                builder.AddObjectSrc().None();
                builder.AddFormAction().Self();
                builder.AddFrameAncestors().None();
            })
            .AddCrossOriginOpenerPolicy(builder =>
            {
                builder.SameOrigin();
            })
            .AddCrossOriginEmbedderPolicy(builder =>
            {
                builder.RequireCorp();
            })
            .AddCrossOriginResourcePolicy(builder =>
            {
                builder.SameOrigin();
            })
            .AddCustomHeader("X-My-Test-Header", "Test header value"));

        return applicationBuilder;
    }
}