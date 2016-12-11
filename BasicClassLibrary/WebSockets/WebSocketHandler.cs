using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace BCL.WebSockets
{
    public class WebSocketHandler
    {
        private WebSocket _webSocket;
        int frameBytesCount = 10 * 1024;

        public virtual async Task ProcessRequest(AspNetWebSocketContext context)
        {
            _webSocket = context.WebSocket;
            
            RaiseOpenEvent();

            while (_webSocket.State == WebSocketState.Open)
            {
                var receivedBytes = new List<byte>();
                var buffer = WebSocket.CreateServerBuffer(frameBytesCount);
                WebSocketMessageType messageType = WebSocketMessageType.Text;

                WebSocketReceiveResult receiveResult = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
                messageType = receiveResult.MessageType;
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    RaiseOnClosed();
                    break;
                }

                MergeFrameContent(receivedBytes, buffer.Array, receiveResult.Count);

                while (!receiveResult.EndOfMessage)
                {
                    buffer = WebSocket.CreateServerBuffer(frameBytesCount);
                    receiveResult = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    MergeFrameContent(receivedBytes, buffer.Array, receiveResult.Count);
                }

                RaiseMessageArrive(receivedBytes.ToArray(), messageType, receivedBytes.Count);
            }
        }

        public virtual async Task SendMessage(string message)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("the web socket is not open.");
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            int sentBytes = 0;
            while (sentBytes < bytes.Length)
            {
                int remainingBytes = bytes.Length - sentBytes;
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes,sentBytes,remainingBytes > frameBytesCount ? frameBytesCount : remainingBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                sentBytes += frameBytesCount;
            }
        }

        public virtual async Task Close()
        {
            if (_webSocket == null || _webSocket.State == WebSocketState.Closed || _webSocket.State == WebSocketState.Aborted)
            {
                return;
            }

            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server Close", CancellationToken.None);
        }

        public event EventHandler Opened;

        public event EventHandler<string> TextMessageReceived;

        public event EventHandler Closed;

        protected void MergeFrameContent(List<Byte> destBuffer,byte[] buffer,long count)
        {
            count = count < buffer.Length ? count : buffer.Length;

            if (count == buffer.Length)
            {
                destBuffer.AddRange(buffer);
            }
            else
            {
                var frameBuffer = new byte[count];
                Array.Copy(buffer, frameBuffer, count);

                destBuffer.AddRange(frameBuffer);
            }
        }

        protected void RaiseOpenEvent()
        {
            if (Opened != null)
            {
                Opened(this, EventArgs.Empty);
            }
        }

        protected void RaiseMessageArrive(byte[] buffer, WebSocketMessageType type,long count)
        {
            if (type == WebSocketMessageType.Text && TextMessageReceived != null)
            {
                TextMessageReceived(this, Encoding.UTF8.GetString(buffer));
            }
        }

        protected void RaiseOnClosed()
        {
            if (Closed != null)
            {
                Closed(this, EventArgs.Empty);
            }
        }
    }
}