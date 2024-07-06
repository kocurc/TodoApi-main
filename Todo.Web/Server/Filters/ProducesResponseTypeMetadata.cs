using System;
using Microsoft.AspNetCore.Http.Metadata;
using System.Collections.Generic;

namespace Todo.Web.Server.Filters;

public sealed class ProducesResponseTypeMetadata(Type type, int statusCode, string contentType) : IProducesResponseTypeMetadata
{
    public Type Type { get; } = type;
    public int StatusCode { get; } = statusCode;
    public IEnumerable<string> ContentTypes { get; } = [contentType];
}
