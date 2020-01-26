using System.Collections.Generic;

namespace WebsocketClient {
    public class WebSocketRequest {
        public long Seq { get; set; }
        public string Action { get; set; }
        public WebSocketRequestData Data { get; set; }
    }

    public class WebSocketRequestData {
        public string Token { get; set; }
    }

    public class WebSocketResponse {
        public string Status { get; set; }
        public string Event { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public Dictionary<string, string> Broadcast { get; set; }
        public Dictionary<string, string> Error { get; set; }
        public long Seq { get; set; }
        public long SeqReply { get; set; }
    }

    public class Posted {
        public string Id { get; set; }
        public string RootId { get; set; }
        public string UserId { get; set; }
        public string ChannelId { get; set; }
        public string Message { get; set; }

        public string GetReplyPostId() {
            if (!string.IsNullOrWhiteSpace(RootId)) return RootId;
            return Id;
        }
    }

    public class PostedEvent {
        public string ChannelDisplayName { get; set; }
        public Posted Post { get; set; }
    }
}