using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Language.Flow;

namespace TodoApi.Tests.Builders;

public class HttpClientMockBuilder
{
    private readonly ISetup<HttpClient, Task<HttpResponseMessage>> _setup;

    private Mock<HttpClient> _httpClientMock;
    private HttpResponseMessage _responseMessage;

    public HttpClientMockBuilder()
    {
        _httpClientMock = new Mock<HttpClient>();
        _setup = _httpClientMock
            .Setup(httpClient => httpClient.SendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()));
        _responseMessage = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.BadRequest
        };
    }

    public HttpClientMockBuilder WithResponse(HttpStatusCode httpStatusCode)
    {
        _responseMessage = new HttpResponseMessage
        {
            StatusCode = httpStatusCode
        };

        return this;
    }

    public void Reset()
    {
        _httpClientMock = new Mock<HttpClient>();
    }

    public Mock<HttpClient> Build()
    {
        _setup.ReturnsAsync(_responseMessage);

        return _httpClientMock;
    }
}
