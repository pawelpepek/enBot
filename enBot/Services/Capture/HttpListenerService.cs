using enBot.Models;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace enBot.Services.Capture;

public class HttpListenerService : IDisposable
{
    private readonly HttpListener _listener = new();
    private CancellationTokenSource _cts;
    private Task _loopTask;

    public Action<RawPrompt> OnRawPromptReceived { get; set; }

    public HttpListenerService(string prefix = "http://localhost:5151/")
    {
        _listener.Prefixes.Add(prefix);
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Start();
        _loopTask = Task.Run(ListenLoop);
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listener.Stop(); } catch { /* ignore */ }
        try { _loopTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _listener.Close();
        _loopTask?.Dispose();
    }

    private async Task ListenLoop()
    {
        var cts = _cts;
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = HandleRequestAsync(context);
            }
        }
        catch (HttpListenerException) when (cts.IsCancellationRequested)
        {
            // Expected on shutdown
        }
        catch (ObjectDisposedException) when (cts.IsCancellationRequested)
        {
            // Expected on shutdown
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.HttpMethod != "POST" || request.Url?.AbsolutePath != "/hook")
        {
            await WriteResponseAsync(response, 404, "Not found").ConfigureAwait(false);
            return;
        }

        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            var rawPrompt = JsonSerializer.Deserialize<RawPrompt>(body);

            if (rawPrompt is null || string.IsNullOrWhiteSpace(rawPrompt.Original))
            {
                await WriteResponseAsync(response, 400, "Invalid or empty prompt").ConfigureAwait(false);
                return;
            }

            OnRawPromptReceived?.Invoke(rawPrompt);
            await WriteResponseAsync(response, 200, "OK").ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            await WriteResponseAsync(response, 400, $"JSON parse error: {ex.Message}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await WriteResponseAsync(response, 500, $"Internal error: {ex.Message}").ConfigureAwait(false);
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, int statusCode, string body)
    {
        response.StatusCode = statusCode;
        response.ContentType = "text/plain";
        var bytes = Encoding.UTF8.GetBytes(body);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        response.Close();
    }
}
