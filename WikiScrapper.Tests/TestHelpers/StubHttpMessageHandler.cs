using System.Net;

namespace WikiScrapper.Tests.TestHelpers;

/// <summary>
/// A test double for <see cref="HttpMessageHandler"/> that returns canned responses
/// and records the requests it receives.
/// </summary>
public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    : HttpMessageHandler
{
    /// <summary>All requests received by the handler, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Creates a handler that always returns the same response.</summary>
    public static StubHttpMessageHandler Returning(HttpStatusCode statusCode, string? jsonContent = null) =>
        new(_ =>
        {
            var response = new HttpResponseMessage(statusCode);
            if (jsonContent is not null)
            {
                response.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            }
            return response;
        });

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(responder(request));
    }
}
