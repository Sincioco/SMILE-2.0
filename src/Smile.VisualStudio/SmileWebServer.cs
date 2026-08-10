using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Smile.VisualStudio;

internal static class SmileWebServer
{
    private static readonly object Gate = new();
    private static TcpListener? _listener;
    private static CancellationTokenSource? _cancellation;
    private static string? _root;
    private static int _port;

    public static string Start(string outputDirectory)
    {
        var root = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        lock (Gate)
        {
            if (_listener == null || !string.Equals(_root, root, StringComparison.OrdinalIgnoreCase))
            {
                StopCore();
                _root = root;
                _cancellation = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _ = Task.Run(() => ListenAsync(_listener, root, _cancellation.Token));
            }

            return $"http://127.0.0.1:{_port}/?v={DateTime.UtcNow.Ticks}";
        }
    }

    private static async Task ListenAsync(TcpListener listener, string root, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _ = Task.Run(() => ServeAsync(client, root), cancellationToken);
        }
    }

    private static async Task ServeAsync(TcpClient client, string root)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
        {
            try
            {
                var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(requestLine))
                    return;
                var parts = requestLine.Split(' ');
                if (parts.Length < 2 || (parts[0] != "GET" && parts[0] != "HEAD"))
                {
                    await WriteErrorAsync(stream, 405, "Method Not Allowed").ConfigureAwait(false);
                    return;
                }

                string? line;
                do { line = await reader.ReadLineAsync().ConfigureAwait(false); }
                while (!string.IsNullOrEmpty(line));

                var requestPath = parts[1].Split('?')[0].Replace('/', Path.DirectorySeparatorChar);
                requestPath = Uri.UnescapeDataString(requestPath).TrimStart(Path.DirectorySeparatorChar);
                if (string.IsNullOrWhiteSpace(requestPath))
                    requestPath = "index.html";

                var filePath = Path.GetFullPath(Path.Combine(root, requestPath));
                var rootPrefix = root + Path.DirectorySeparatorChar;
                if (!filePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
                {
                    await WriteErrorAsync(stream, 404, "Not Found").ConfigureAwait(false);
                    return;
                }

                var content = File.ReadAllBytes(filePath);
                var headers = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\n" +
                    $"Content-Type: {MimeType(Path.GetExtension(filePath))}\r\n" +
                    $"Content-Length: {content.Length}\r\n" +
                    "Cache-Control: no-store, no-cache, must-revalidate\r\n" +
                    "Pragma: no-cache\r\n" +
                    "X-Content-Type-Options: nosniff\r\n" +
                    "Connection: close\r\n\r\n");
                await stream.WriteAsync(headers, 0, headers.Length).ConfigureAwait(false);
                if (parts[0] == "GET")
                    await stream.WriteAsync(content, 0, content.Length).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException || exception is SocketException || exception is UriFormatException || exception is ArgumentException)
            {
                // The browser may close speculative requests; the listener remains available.
            }
        }
    }

    private static async Task WriteErrorAsync(Stream stream, int status, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var headers = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {status} {message}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {body.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers, 0, headers.Length).ConfigureAwait(false);
        await stream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
    }

    private static string MimeType(string extension)
    {
        var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".js"] = "text/javascript; charset=utf-8",
            [".css"] = "text/css; charset=utf-8",
            [".json"] = "application/json; charset=utf-8",
            [".txt"] = "text/plain; charset=utf-8",
            [".wav"] = "audio/wav",
            [".mp3"] = "audio/mpeg",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".svg"] = "image/svg+xml",
            [".webp"] = "image/webp",
            [".ico"] = "image/x-icon"
        };
        return types.TryGetValue(extension, out var type) ? type : "application/octet-stream";
    }

    private static void StopCore()
    {
        _cancellation?.Cancel();
        _listener?.Stop();
        _cancellation?.Dispose();
        _cancellation = null;
        _listener = null;
        _root = null;
        _port = 0;
    }
}
