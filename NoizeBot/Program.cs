﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using WebsocketClient;

namespace NoizeBot {
    internal class Program {
        private static CancellationToken _cancellationToken;
        private static Configuration _configuration;
        private static readonly Channel<PostedEvent> PostedChannel = Channel.CreateUnbounded<PostedEvent>();
        public static Action Shutdown;
        public static string BotUserId { get; set; }
        private static void Main(string[] args) {
            try {
                _configuration = new Configuration();
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) => {
                    if (cts.IsCancellationRequested) return;
                    Console.WriteLine("Shutdown...");
                    cts.Cancel(false);
                    e.Cancel = true;
                };
                _cancellationToken = cts.Token;
                Shutdown = () => cts.Cancel(false);
                MainAsync().GetAwaiter().GetResult();
            } catch (Exception e) {
                if (_cancellationToken.IsCancellationRequested) {
                    Console.WriteLine(e.Message);
                    Environment.Exit(-1);
                }

                throw;
            }
        }
        public static void KillMe() {
            try { Shutdown?.Invoke();} catch { /*nah...*/ }
            try { Environment.Exit(99); } catch { /*nah...*/}
            Environment.FailFast(null);
        }
        private static async Task MainAsync() {
            if (_configuration.Repl) {
                Repl();
                return;
            }

            var url = _configuration.ServerUri.TrimEnd('/') + "/api/v4/websocket";
            url = Regex.Replace(url, "^https:", "wss:");
            url = Regex.Replace(url, "^http:", "ws:");
            using var socket = new MattermostWebsocket(new Uri(url), cancellationToken: _cancellationToken);
            socket.OnPosted += e => {
                Console.WriteLine($"{e.ChannelDisplayName}> {e.Post.Message}");
                if (BotUserId == e.Post.UserId) return;
                if (e.Post.Message == "nb_kill")
                    try {
                        Processing.CreatePost(":dizzy_face:", e.Post.ChannelId, e.Post.GetReplyPostId());
                    } finally {
                        KillMe();
                    }
                ;
                PostedChannel.Writer.WriteAsync(e, _cancellationToken).GetAwaiter().GetResult();
            };
            socket.OnHello += sv => { Console.WriteLine($"ServerVersion: {sv}"); };
            socket.OnBotUserId += id => { BotUserId = id; };
            socket.OnWebSocketResponse += r => {
                if (!string.IsNullOrEmpty(r.Status)) Console.WriteLine($"Status: {r.Status}");
                if (!string.IsNullOrEmpty(r.Event)) Console.WriteLine($"Event: {r.Event}");
                if (r.Error != null && r.Error.Any())
                    foreach (var (k, v) in r.Error) {
                        Console.WriteLine($"ERROR: {k}:{v}");
                    }
            };
            if (_configuration.Verbose) socket.OnJsonMessage += Console.WriteLine;
            await socket.Authenticate(_configuration.Token);
            var processingTask = Process();
            var listenTask = socket.Listen();
            await Task.WhenAny(processingTask, listenTask);
        }
        private static async Task Process() {
            Regex ignoreChannelsRegex = null;
            if (!string.IsNullOrWhiteSpace(_configuration.IgnoreChannelsRegex)) ignoreChannelsRegex = new Regex(_configuration.IgnoreChannelsRegex);
            await foreach (var e in PostedChannel.Reader.ReadAllAsync()) {
                try {
                    if (string.IsNullOrWhiteSpace(e?.Post?.Message)) continue;

                    if (ignoreChannelsRegex != null && !string.IsNullOrWhiteSpace(e.ChannelDisplayName))
                        if (ignoreChannelsRegex.Match(e.ChannelDisplayName).Success)
                            continue;

                    var message = e.Post.Message.Trim();
                    Processing.Process(message, e.ChannelDisplayName, e.Post.ChannelId, e.Post.GetReplyPostId());
                } catch (Exception ex) {
                    if (_configuration.Verbose)
                        Console.WriteLine(ex.ToString());
                    else
                        Console.WriteLine(ex);
                }
            }
        }
        private static void Repl() {
            Console.WriteLine("Repl...\nEnter message:");
            while (!_cancellationToken.IsCancellationRequested) {
                Console.Write(">");
                var line = Console.ReadLine();
                if (line == null) break;
                Processing.Process(line, "channel", "", "");
            }
        }
    }
}