using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;

namespace WebSocketSample.Controllers
{
    [RoutePrefix("api/chat")]
    public class ChatController : ApiController
    {
        private static List<WebSocket> _sockets = new List<WebSocket>();

        [Route]
        [HttpGet]
        public HttpResponseMessage Connect(string nickName)
        {
            HttpContext.Current.AcceptWebSocketRequest(ProcessRequest);

            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }

        public async Task ProcessRequest(AspNetWebSocketContext context)
        {
            var socket = context.WebSocket;
            _sockets.Add(socket);

            while (true)
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                var receivedResult = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (receivedResult.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
                    _sockets.Remove(socket);
                    break;
                }

                if (socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    string recvMsg = Encoding.UTF8.GetString(buffer.Array, 0, receivedResult.Count);
                    var recvBytes = Encoding.UTF8.GetBytes(recvMsg);
                    var sendBuffer = new ArraySegment<byte>(recvBytes);
                    foreach (var innerSocket in _sockets)
                    {
                        if (innerSocket != socket)
                        {
                            await innerSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
            }
        }
    }
}
