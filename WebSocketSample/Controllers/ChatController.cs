using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using BCL.WebSockets;

namespace WebSocketSample.Controllers
{
    [RoutePrefix("api/chat")]
    public class ChatController : ApiController
    {
        private static Dictionary<string, WebSocketHandler> _handlers = new Dictionary<string, WebSocketHandler>();

        [Route]
        [HttpGet]
        public async Task<HttpResponseMessage> Connect(string nickName)
        {
            if (string.IsNullOrEmpty(nickName))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var webSocketHandler = new WebSocketHandler();
            if (_handlers.ContainsKey(nickName))
            {
                var origHandler = _handlers[nickName];
                await origHandler.Close();
            }

            _handlers[nickName] = webSocketHandler;

            webSocketHandler.TextMessageReceived += ((sendor, msg) =>
            {
                BroadcastMessage(nickName, nickName + "Says: " + msg);
            });

            webSocketHandler.Closed += (sendor, arg) =>
            {
                BroadcastMessage(nickName, nickName + " Disconnected!");
                _handlers.Remove(nickName);
            };

            webSocketHandler.Opened += (sendor, arg) =>
                        {
                            BroadcastMessage(nickName, nickName + " Connected!");
                        };

            HttpContext.Current.AcceptWebSocketRequest(webSocketHandler);

            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }

        private void BroadcastMessage(string sendorNickName, string message)
        {
            foreach (var handlerKvp in ChatController._handlers)
            {
                if (handlerKvp.Key != sendorNickName)
                {
                    handlerKvp.Value.SendMessage(message).Wait();
                }
            }
        }
    }
}
