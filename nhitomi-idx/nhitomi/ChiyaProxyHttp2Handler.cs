using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Http2;
using Http2.Hpack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    /// <summary>
    /// Implements <see cref="HttpMessageHandler"/> for a proprietary HTTP2 proxy written by chiya.dev.
    /// </summary>
    /// <remarks>
    /// chiya.dev proxy server is proprietary, but this client implementation is a standard-conforming HTTP2 request/response handler with no bells and whistles.
    /// Although untested, anyone should be able to use this client with any unencrypted HTTP2 proxy server.
    /// </remarks>
    public class ChiyaProxyHttp2Handler : HttpMessageHandler
    {
        readonly IPEndPoint _endPoint;
        readonly IOptionsMonitor<ProxyOptions> _options;
        readonly ILogger<ChiyaProxyHttp2Handler> _logger;

        public ChiyaProxyHttp2Handler(IPEndPoint endPoint, IOptionsMonitor<ProxyOptions> options, ILogger<ChiyaProxyHttp2Handler> logger)
        {
            _endPoint = endPoint;
            _options  = options;
            _logger   = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var options   = _options.CurrentValue;
            var response  = null as HttpResponseMessage;
            var exception = null as Exception;

            for (var i = 0; i < options.RetryCount + 1; i++)
            {
                var waited = false;

                try
                {
                    response = await SendAsyncInternal(request, cancellationToken);

                    switch (response.StatusCode)
                    {
                        // retry on certain statuses
                        case HttpStatusCode.TooManyRequests:
                        case HttpStatusCode.InternalServerError:
                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                            break;

                        default:
                            return response;
                    }

                    // use retry-after header
                    var retry = response.Headers.RetryAfter;
                    var date  = retry?.Date?.UtcDateTime;
                    var delta = retry?.Delta;

                    if (date > DateTime.UtcNow)
                    {
                        await Task.Delay(date.Value - DateTime.UtcNow, cancellationToken);
                        waited = true;
                    }

                    else if (delta != null)
                    {
                        await Task.Delay(delta.Value, cancellationToken);
                        waited = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    exception ??= e;
                }

                if (!waited)
                    await Task.Delay(TimeSpan.FromSeconds(i + 1), cancellationToken);

                // request needs to be cloned for resend
                var newRequest = await CloneAsync(request);

                request.Dispose();
                request = newRequest;
            }

            if (exception != null)
                ExceptionDispatchInfo.Throw(exception);

            return response;
        }

        static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            foreach (var property in request.Properties)
                clone.Properties.Add(property);

            foreach (var (header, value) in request.Headers)
                clone.Headers.TryAddWithoutValidation(header, value);

            if (request.Content != null)
            {
                var memory = new MemoryStream();

                await request.Content.CopyToAsync(memory);

                memory.Position = 0;

                clone.Content = new StreamContent(memory);

                foreach (var (header, value) in request.Content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(header, value);
            }

            return clone;
        }

        readonly SemaphoreSlim _initLock = new SemaphoreSlim(1);
        volatile TcpClient _client;
        volatile Connection _connection;

        async ValueTask<Connection> InitializeConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = _connection;

            if (connection?.IsExhausted == false)
                return connection;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                connection = _connection;

                if (connection?.IsExhausted == false)
                    return connection;

                var client = new TcpClient
                {
                    SendBufferSize    = _bodyBufferSize,
                    ReceiveBufferSize = _bodyBufferSize
                };

                try
                {
                    await client.ConnectAsync(_endPoint.Address, _endPoint.Port);

                    var streams = client.Client.CreateStreams();

                    connection = new Connection(
                        new ConnectionConfigurationBuilder(false)
                           .UseSettings(Settings.Max)
                           .Build(),
                        streams.ReadableStream,
                        streams.WriteableStream,
                        new Connection.Options
                        {
                            Logger = _logger
                        });

                    await connection.PingAsync(cancellationToken);

                    Interlocked.Exchange(ref _client, client)?.Dispose();
                    _connection = connection;

                    return connection;
                }
                catch
                {
                    client.Dispose();
                    throw;
                }
            }
            finally
            {
                try
                {
                    _initLock.Release();
                }
                catch (ObjectDisposedException) { }
            }
        }

        const int _bodyBufferSize = 32768;
        static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        async Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestHeaders = new List<HeaderField>(8)
            {
                new HeaderField
                {
                    Name  = ":method",
                    Value = request.Method.Method
                },
                new HeaderField
                {
                    Name  = ":scheme",
                    Value = request.RequestUri.Scheme
                },
                new HeaderField
                {
                    Name  = ":path",
                    Value = request.RequestUri.PathAndQuery
                },
                new HeaderField
                {
                    Name  = ":authority",
                    Value = request.RequestUri.Authority
                }
            };

            foreach (var (header, values) in request.Headers)
                requestHeaders.Add(new HeaderField { Name = header.ToLowerInvariant(), Value = string.Join(',', values) });

            if (request.Content != null)
                foreach (var (header, values) in request.Content.Headers)
                    requestHeaders.Add(new HeaderField { Name = header.ToLowerInvariant(), Value = string.Join(',', values) });

            await using var requestBody = request.Content == null ? null : await request.Content.ReadAsStreamAsync();

            if (requestBody != null)
            {
                var hasLength = false;

                foreach (var header in requestHeaders)
                {
                    if (header.Name == "content-length")
                    {
                        hasLength = true;
                        break;
                    }
                }

                try
                {
                    if (!hasLength)
                        requestHeaders.Add(new HeaderField { Name = "content-length", Value = requestBody.Length.ToString() });
                }
                catch (NotSupportedException)
                {
                    // length may not be available
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var connection = await InitializeConnectionAsync(cancellationToken);
            var stream     = await connection.CreateStreamAsync(requestHeaders, requestBody == null);

            try
            {
                // ReSharper disable once AccessToDisposedClosure
                await using (cancellationToken.Register(stream.Dispose))
                {
                    if (requestBody != null)
                    {
                        var buffer = _bufferPool.Rent(_bodyBufferSize);

                        try
                        {
                            while (true)
                            {
                                var read = await requestBody.ReadAsync(new Memory<byte>(buffer, 0, _bodyBufferSize), cancellationToken);

                                if (read == 0)
                                {
                                    await stream.CloseAsync();
                                    break;
                                }

                                await stream.WriteAsync(new ArraySegment<byte>(buffer, 0, read));
                            }
                        }
                        finally
                        {
                            _bufferPool.Return(buffer);

                            await requestBody.DisposeAsync();
                        }
                    }

                    var response = new HttpResponseMessage
                    {
                        RequestMessage = request,
                        Version        = request.Version
                    };

                    try
                    {
                        var responseStream = new Http2ResponseStream(stream, response);
                        response.Content = new StreamContent(responseStream);

                        foreach (var header in await stream.ReadHeadersAsync())
                        {
                            response.Headers.TryAddWithoutValidation(header.Name, header.Value);
                            response.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);

                            switch (header.Name)
                            {
                                case ":status" when int.TryParse(header.Value, out var s):
                                    response.StatusCode = (HttpStatusCode) s;
                                    break;

                                case "content-length" when long.TryParse(header.Value, out var cl):
                                    responseStream.LengthNullable = cl;
                                    break;
                            }
                        }

                        return response;
                    }
                    catch
                    {
                        response.Dispose();
                        throw;
                    }
                }
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        sealed class Http2ResponseStream : Stream
        {
            readonly IStream _stream;
            readonly HttpResponseMessage _message;

            public long? LengthNullable;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => LengthNullable ?? throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public Http2ResponseStream(IStream stream, HttpResponseMessage message)
            {
                _stream  = stream;
                _message = message;
            }

            public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count).GetAwaiter().GetResult();

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var result = await _stream.ReadAsync(new ArraySegment<byte>(buffer, offset, count));

                if (result.EndOfStream)
                    foreach (var trailer in await _stream.ReadTrailersAsync())
                        _message.TrailingHeaders.TryAddWithoutValidation(trailer.Name, trailer.Value);

                return result.BytesRead;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
            {
                // unfortunately we need a secondary buffer because http2dotnet won't take memory<byte>
                var buffer2 = _bufferPool.Rent(buffer.Length);

                try
                {
                    var result = await _stream.ReadAsync(new ArraySegment<byte>(buffer2, 0, buffer.Length)); // original buffer (not buffer2) length!

                    ((Memory<byte>) buffer2).Slice(0, result.BytesRead).CopyTo(buffer);

                    if (result.EndOfStream)
                        foreach (var trailer in await _stream.ReadTrailersAsync())
                            _message.TrailingHeaders.TryAddWithoutValidation(trailer.Name, trailer.Value);

                    return result.BytesRead;
                }
                finally
                {
                    _bufferPool.Return(buffer2);
                }
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
            public override void WriteByte(byte value) => throw new NotSupportedException();
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken()) => throw new NotSupportedException();

            public override void Flush() => throw new NotSupportedException();

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                var bufferSrc = _bufferPool.Rent(_bodyBufferSize);

                try
                {
                    while (true)
                    {
                        var result = await _stream.ReadAsync(new ArraySegment<byte>(bufferSrc, 0, _bodyBufferSize));

                        await destination.WriteAsync(new Memory<byte>(bufferSrc, 0, result.BytesRead), cancellationToken);

                        if (result.EndOfStream)
                        {
                            foreach (var trailer in await _stream.ReadTrailersAsync())
                                _message.TrailingHeaders.TryAddWithoutValidation(trailer.Name, trailer.Value);

                            break;
                        }
                    }
                }
                finally
                {
                    _bufferPool.Return(bufferSrc);
                }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                    _stream.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _initLock.Dispose();

                Interlocked.Exchange(ref _client, null)?.Dispose();
                _connection = null;
            }
        }
    }
}