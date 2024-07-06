using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MiniValidation;
using ProducesResponseTypeMetadata = Todo.Web.Server.Filters.ProducesResponseTypeMetadata;

namespace Todo.Web.Server.Extensions;

public static class TBuilderExtensions
{
    public static TBuilder WithParameterValidation<TBuilder>(this TBuilder builder, params Type[] typesToValidate) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(eb =>
        {
            var methodInfo = eb.Metadata.OfType<MethodInfo>().FirstOrDefault();

            if (methodInfo is null)
            {
                return;
            }

            List<int>? parameterIndexesToValidate = null;

            foreach (var p in methodInfo.GetParameters())
            {
                if (!typesToValidate.Contains(p.ParameterType))
                {
                    continue;
                }

                parameterIndexesToValidate ??= [];
                parameterIndexesToValidate.Add(p.Position);
            }

            if (parameterIndexesToValidate is null)
            {
                return;
            }

            eb.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), 400, "application/problem+json"));
            eb.FilterFactories.Add((context, next) =>
            {
                return endpointFilterInvocationContext =>
                {
                    foreach (var index in parameterIndexesToValidate)
                    {
                        if (endpointFilterInvocationContext.Arguments[index] is { } arg && !MiniValidator.TryValidate(arg, out var errors))
                        {
                            return new ValueTask<object?>(Results.ValidationProblem(errors));
                        }
                    }

                    return next(endpointFilterInvocationContext);
                };
            });
        });

        return builder;
    }
}
