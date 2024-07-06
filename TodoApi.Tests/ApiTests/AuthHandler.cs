using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Todo.Tests.ApiTests;

public sealed class AuthHandler(Action<HttpRequestMessage> onRequest) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        onRequest(request);

        return base.SendAsync(request, cancellationToken);
    }
}
