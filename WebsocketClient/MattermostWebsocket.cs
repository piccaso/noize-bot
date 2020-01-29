using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketClient {
    public class MattermostWebsocket : IDisposable {
        private readonly Uri _socketUrl;
        private readonly CancellationToken _cancellationToken;
        private readonly ClientWebSocket _socket = new ClientWebSocket();
        private readonly byte[] _socketBuffer;
        private readonly JsonSerializerOptions _serializerOptions;
        private long _seq;
        private readonly Encoding _encoding = Encoding.UTF8;

        public MattermostWebsocket(Uri socketUrl, int bufferSize = 8 * 1024, IWebProxy proxy = null,
            CancellationToken cancellationToken = default) {
            _socketUrl = socketUrl;
            _cancellationToken = cancellationToken;
            _socketBuffer = new byte[bufferSize];
            if (proxy != null) _socket.Options.Proxy = proxy;
            _serializerOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
            };
        }

        private long NextSeq() {
            unchecked {
                _seq++;
            }

            return _seq;
        }

        private string Serialize(object o) {
            return JsonSerializer.Serialize(o, _serializerOptions);
        }

        private bool TryDeserialize<T>(string json, out T obj) {
            try {
                obj = JsonSerializer.Deserialize<T>(json, _serializerOptions);
                return !(obj is null);
            }
            catch {
                obj = default;
            }

            return false;
        }

        private async Task ConnectAsync() {
            if (_socket.State == WebSocketState.Open) return;
            await _socket.ConnectAsync(_socketUrl, _cancellationToken);
        }
        
        private async Task SendAsync(string message) {
            await ConnectAsync();
            var buffer = _encoding.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);
            await _socket.SendAsync(segment, WebSocketMessageType.Text, true, _cancellationToken);
        }

        public async Task Authenticate(string token) {
            var msg = new WebSocketRequest {
                Action = "authentication_challenge",
                Data = new WebSocketRequestData {
                    Token = token
                },
                Seq = NextSeq(),
            };
            await SendAsync(Serialize(msg));
        }
        
        public async Task GetStatuses() {
            var msg = new WebSocketRequest {
                Action = "get_statuses",
                Seq = NextSeq(),
            };
            await SendAsync(Serialize(msg));
        }

        private async Task<string> ReceiveAsync() {
            await using var ms = new MemoryStream();
            var segment = new ArraySegment<byte>(_socketBuffer, 0, _socketBuffer.Length);
            bool endOfMessage;
            do {
                var result = await _socket.ReceiveAsync(segment, _cancellationToken);
                endOfMessage = result.EndOfMessage;
                if (result.Count > 0) {
                    ms.Write(segment.AsSpan(0, result.Count));
                }
            } while (!endOfMessage);

            return _encoding.GetString(ms.ToArray());
        }

        public event Action<string> OnJsonMessage;
        public event Action<WebSocketResponse> OnWebSocketResponse;
        public event Action<PostedEvent> OnPosted;
        public event Action<string> OnHello;
        public event Action<string> OnBotUserId;

        public async Task Listen() {
            while (!_cancellationToken.IsCancellationRequested) {
                var message = await ReceiveAsync();
                OnJsonMessage?.Invoke(message);
                if (TryDeserialize<WebSocketResponse>(message, out var response)) {
                    var channelDisplayName = response.GetDataOrDefault("channel_display_name");
                    OnWebSocketResponse?.Invoke(response);
                    if (response.TryGetData("posted", "post", out var postJson)) {
                        if (TryDeserialize<Posted>(postJson, out var posted)) {
                            OnPosted?.Invoke(new PostedEvent {
                                ChannelDisplayName = channelDisplayName,
                                Post = posted
                            });
                        }
                    }
                    if (response.TryGetData("hello", "server_version", out var serverVersion)) {
                        OnHello?.Invoke(serverVersion);
                        if (response.Broadcast != null && response.Broadcast.ContainsKey("user_id")) {
                            OnBotUserId?.Invoke(response.Broadcast["user_id"]);
                        }
                    }
                }
            }
        }

        public void Dispose() {
            _socket.Dispose();
        }
    }

    public static class WebSocketResponseExtensions {
        public static bool TryGetData(this WebSocketResponse r, string @event, string key, out string data) {
            data = null;
            return r?.Data != null && r.Event == @event && r.Data.TryGetValue(key, out data);
        }

        public static string GetDataOrDefault(this WebSocketResponse r, string key) {
            if (r?.Data != null && r.Data.TryGetValue(key, out var data)) {
                return data;
            }

            return null;
        }
    }
}