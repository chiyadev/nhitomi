using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Http2;
using Http2.Hpack;
using Microsoft.Extensions.Logging;

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
        readonly ILogger<ChiyaProxyHttp2Handler> _logger;

        public ChiyaProxyHttp2Handler(IPEndPoint endPoint, ILogger<ChiyaProxyHttp2Handler> logger)
        {
            _endPoint = endPoint;
            _logger   = logger;
        }

        readonly SemaphoreSlim _initLock = new SemaphoreSlim(1);
        volatile TcpClient _client;
        volatile Connection _connection;

        async ValueTask<Connection> InitializeConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = _connection;

            if (connection != null)
                return connection;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                connection = _connection;

                if (connection != null)
                    return connection;

                var client = new TcpClient();

                try
                {
                    await client.ConnectAsync(_endPoint.Address, _endPoint.Port);

                    var streams = client.Client.CreateStreams();

                    connection = new Connection(
                        new ConnectionConfigurationBuilder(false)
                           .UseSettings(Settings.Default)
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

        const int _bodyBufferSize = 4096;
        static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
                                    break;

                                await stream.WriteAsync(new ArraySegment<byte>(buffer, 0, read));
                            }
                        }
                        finally
                        {
                            _bufferPool.Return(buffer);

                            await requestBody.DisposeAsync();
                        }

                        await stream.CloseAsync();
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
                // unfortunately we need a secondary byte[] buffer
                var buffer2 = _bufferPool.Rent(buffer.Length);

                try
                {
                    var result = await _stream.ReadAsync(new ArraySegment<byte>(buffer2, 0, buffer2.Length));

                    buffer2.CopyTo(buffer.Slice(0, result.BytesRead));

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