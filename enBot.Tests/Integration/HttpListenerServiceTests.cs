using enBot.Models;
using enBot.Services.Capture;
using System.Net;
using System.Text;

namespace enBot.Tests.Integration;

public class HttpListenerServiceTests : IDisposable
{
    // Use a unique port per test run to avoid conflicts
    private const string Prefix = "http://localhost:5252/";
    private readonly HttpListenerService _svc;
    private readonly HttpClient _client = new();

    public HttpListenerServiceTests()
    {
        _svc = new HttpListenerService(Prefix);
        _svc.Start();
    }

    public void Dispose()
    {
        _svc.Dispose();
        _client.Dispose();
    }

    private Task<HttpResponseMessage> Post(string path, string body) =>
        _client.PostAsync(Prefix.TrimEnd('/') + path,
            new StringContent(body, Encoding.UTF8, "application/json"));

    [Fact]
    public async Task ValidHookPost_Returns200_AndFiresEvent()
    {
        RawPrompt received = null;
        _svc.OnRawPromptReceived = p => received = p;

        var response = await Post("/hook", "{\"original\":\"hello world\"}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Task.Delay(50); // give fire-and-forget time to complete
        Assert.NotNull(received);
        Assert.Equal("hello world", received!.Original);
    }

    [Fact]
    public async Task EmptyOriginal_Returns400_NoEvent()
    {
        var fired = false;
        _svc.OnRawPromptReceived = _ => fired = true;

        var response = await Post("/hook", "{\"original\":\"\"}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await Task.Delay(50); // give fire-and-forget time to complete
        Assert.False(fired);
    }

    [Fact]
    public async Task WhitespaceOriginal_Returns400_NoEvent()
    {
        var fired = false;
        _svc.OnRawPromptReceived = _ => fired = true;

        var response = await Post("/hook", "{\"original\":\"   \"}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(fired);
    }

    [Fact]
    public async Task InvalidJson_Returns400()
    {
        var response = await Post("/hook", "not-json");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WrongPath_Returns404()
    {
        var response = await Post("/wrong", "{\"original\":\"hello\"}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMethod_Returns404()
    {
        var response = await _client.GetAsync(Prefix.TrimEnd('/') + "/hook");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
