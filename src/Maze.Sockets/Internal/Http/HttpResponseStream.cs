using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Maze.Modules.Api.Extensions;
using Maze.Modules.Api.Request;
using Maze.Sockets.Internal.Extensions;
using Maze.Sockets.Internal.Infrastructure;
using Maze.Sockets.Logging;

namespace Maze.Sockets.Internal.Http
{
    public class HttpResponseStream : WriteOnlyStream
    {
        private static readonly ILog Logger = LogProvider.For<HttpResponseStream>();

        private readonly DefaultMazeResponse _response;
        private readonly MazeRequest _request;
        private readonly IDataSocket _socket;
        private readonly int _packageBufferSize;
        private readonly int _maxHeaderSize;
        private readonly ArrayPool<byte> _bufferPool;
        private readonly CancellationToken _requestCancellationToken;
        private bool _isFinalPackage;
        private bool _isFinalPackagePushed;
        private ArraySegment<byte> _latestSendBuffer;
        private bool _disposed;
        private bool _isCompressionEnabled;
        private bool _hasSentPackage;
        private readonly int _customOffset;

        public HttpResponseStream(DefaultMazeResponse response, MazeRequest request, IDataSocket socket,
            int packageBufferSize, int maxHeaderSize, ArrayPool<byte> bufferPool, CancellationToken requestCancellationToken)
        {
            _response = response;
            _request = request;
            _socket = socket;
            _packageBufferSize = packageBufferSize;
            _maxHeaderSize = maxHeaderSize;
            _bufferPool = bufferPool;
            _requestCancellationToken = requestCancellationToken;
            _customOffset = socket.RequiredPreBufferLength ?? 0;
        }

        public async Task FinalFlushAsync()
        {
            _isFinalPackage = true;

            if (_latestSendBuffer == default)
            {
                //if no package was written, the response wasn't started. we have to do that now
                await WriteAsync(new byte[0], 0, 0);
            }

            await SendData(default);

            _isFinalPackagePushed = true;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_response.IsFinished)
                throw new InvalidOperationException("The response is already finished and no new data can be written");

            CancellationTokenSource linkedToken = null;
            if (cancellationToken != CancellationToken.None)
            {
                linkedToken =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _requestCancellationToken);
                cancellationToken = linkedToken.Token;
            }

            using (linkedToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_response.HasStarted)
                {
                    if (!_response.IsCompleted && !_isCompressionEnabled && !_isFinalPackage && !_response.Headers.ContainsKey(HeaderNames.ContentEncoding))
                    {
                        if (_request.GetTypedHeaders().AcceptEncoding?.Contains(new StringWithQualityHeaderValue("gzip")) == true)
                        {
                            _isCompressionEnabled = true;
                            _response.Headers.Add(HeaderNames.ContentEncoding, "gzip");
                            _response.Headers.Remove(HeaderNames.ContentLength);

                            //if the response has not been completed, we compress the body
                            var bodyStream = (BufferingWriteStream) _response.Body;
                            var newPackagingStream = new BufferingWriteStream(this, _packageBufferSize, _bufferPool);

                            var gzipStream = new GZipStream(newPackagingStream, CompressionLevel.Fastest, true);
                            bodyStream.SetInnerStream(gzipStream);

                            bodyStream.FlushCallback = async () =>
                            {
                                gzipStream.Close();
                                await newPackagingStream.FlushAsync();
                            };

                            await gzipStream.WriteAsync(buffer, offset, count, cancellationToken);
                            return;
                        }
                    }

                    _response.StartResponse();

                    if (!_response.Headers.ContainsKey(MazeHeaders.MazeSocketRequestIdHeader)) //automatically set
                        throw new InvalidOperationException(
                            $"Response must have the header {MazeHeaders.MazeSocketRequestIdHeader}");

                    var length = count + _maxHeaderSize + _customOffset;
                    var sendBuffer = _bufferPool.Rent(count + _maxHeaderSize + _customOffset);
                    var headerOffset = HttpFormatter.FormatResponse(_response, new ArraySegment<byte>(sendBuffer, _customOffset, length - _customOffset));
                    if (headerOffset > _maxHeaderSize)
                        throw new InvalidOperationException(
                            $"The header size {headerOffset}B exceeds the maximum allowed header size ({_maxHeaderSize}B)");

                    if (count > 0)
                        Buffer.BlockCopy(buffer, offset, sendBuffer, headerOffset + _customOffset, count);

                    Logger.LogDataPackage("Send HTTP Response", sendBuffer, 0, headerOffset + count);
                    await SendData(new ArraySegment<byte>(sendBuffer, _customOffset, headerOffset + count));
                }
                else
                {
                    var sendBuffer = _bufferPool.Rent(count + 4 + _customOffset);
                    BinaryUtils.WriteInt32(sendBuffer, _customOffset, _response.RequestId);
                    Buffer.BlockCopy(buffer, offset, sendBuffer, 4 + _customOffset, count);

                    await SendData(new ArraySegment<byte>(sendBuffer, _customOffset, count + 4));
                }
            }
        }

        private async Task SendData(ArraySegment<byte> data)
        {
            if (_isFinalPackagePushed)
                throw new InvalidOperationException("Cannot push data after the final one");

            if (_disposed)
                throw new ObjectDisposedException("The stream is disposed and no data can be pushed");

            var latestBuffer = _latestSendBuffer;
            _latestSendBuffer = data;

            if (latestBuffer == default)
                return;

            Logger.LogDataPackage("Send HTTP Response Continuation", latestBuffer.Array, latestBuffer.Offset,
                latestBuffer.Count);

            MazeSocket.MessageOpcode opcode;
            if (_hasSentPackage)
                opcode = _isFinalPackage
                    ? MazeSocket.MessageOpcode.ResponseContinuationFinished
                    : MazeSocket.MessageOpcode.ResponseContinuation;
            else
                opcode = _isFinalPackage
                    ? MazeSocket.MessageOpcode.ResponseSinglePackage
                    : MazeSocket.MessageOpcode.Response;
            
            try
            {
                await _socket.SendFrameAsync(opcode, latestBuffer, _socket.RequiredPreBufferLength.HasValue,
                    CancellationToken.None);
            }
            finally
            {
                _bufferPool.Return(latestBuffer.Array);
            }

            _hasSentPackage = true;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).Wait();
        }

        public override bool CanSeek { get; } = false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            //dont do anything here as we send all data immediately except the latest package
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
                
                if (_latestSendBuffer != default)
                {
                    _bufferPool.Return(_latestSendBuffer.Array);
                    _latestSendBuffer = default;
                }
            }
        }
    }
}
