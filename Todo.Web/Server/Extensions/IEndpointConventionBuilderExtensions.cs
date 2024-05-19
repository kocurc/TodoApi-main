using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;

namespace Todo.Web.Server.Extensions;

// ReSharper disable once InconsistentNaming
public static class IEndpointConventionBuilderExtensions
{
    // Adds the JWT security scheme to the Open API description
    public static IEndpointConventionBuilder AddOpenApiSecurityRequirement(this IEndpointConventionBuilder builder)
    {
        var scheme = new OpenApiSecurityScheme()
        {
            Type = SecuritySchemeType.Http,
            Name = JwtBearerDefaults.AuthenticationScheme,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = JwtBearerDefaults.AuthenticationScheme
            }
        };

        return builder.WithOpenApi(operation => new OpenApiOperation(operation)
        {
            Security =
            {
                new OpenApiSecurityRequirement
                {
                    [scheme] = new List<string>()
                }
            }
        });
    }

    public static IEndpointConventionBuilder RequirePerUserRateLimit(this IEndpointConventionBuilder builder)
    {
        return builder.RequireRateLimiting(Policy);
    }

}