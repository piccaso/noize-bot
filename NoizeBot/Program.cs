using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using WebsocketClient;

namespace NoizeBot {
    internal class Program {
        private static CancellationToken _cancellationToken;
        private static Configuration _configuration;
        private static readonly Channel<PostedEvent> PostedChannel = Channel.CreateUnbounded<PostedEvent>();

        private static void Main(string[] args) {
            try {
                _configuration = new Configuration();
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) => {
                    if (cts.IsCancellationRequested) return;
                    cts.Cancel(false);
                    e.Cancel = true;
                };
                _cancellationToken = cts.Token;
                MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception e) {
                if (_cancellationToken.IsCancellationRequested) {
                    Console.WriteLine(e.Message);
                    Environment.Exit(-1);
                }

                throw;
            }
        }

        private static async Task MainAsync() {
            var url = new Uri(_configuration.WebsocketUrl);
            using var socket = new MattermostWebsocket(url, cancellationToken: _cancellationToken);
            socket.OnPosted += e => {
                Console.WriteLine($"Channel: {e.ChannelDisplayName} Message: {e.Post.Message}");
                PostedChannel.Writer.WriteAsync(e, _cancellationToken);
            };
            socket.OnHello += sv => { Console.WriteLine($"ServerVersion: {sv}"); };
            socket.OnWebSocketResponse += r => {
                if (!string.IsNullOrEmpty(r.Status)) Console.WriteLine($"Status: {r.Status}");
                if (!string.IsNullOrEmpty(r.Event)) Console.WriteLine($"Event: {r.Event}");
                if (r.Error != null && r.Error.Any())
                    foreach (var (k, v) in r.Error)
                        Console.WriteLine($"ERROR: {k}:{v}");
            };
            if (_configuration.Verbose) socket.OnJsonMessage += Console.WriteLine;
            await socket.Authenticate(_configuration.Token);
            var processingTask = Task.Run(Process);
            await socket.Listen();
            await processingTask;
        }

        private static async Task Process() {
            await foreach (var e in PostedChannel.Reader.ReadAllAsync())
                try {
                    if(string.IsNullOrWhiteSpace(e?.Post?.Message)) continue;
                    var msg = e.Post.Message.Trim();
                    if (msg.Contains("Throw up", StringComparison.InvariantCultureIgnoreCase))
                        throw new Exception(e.Post.Message);
                    if (msg.StartsWith("mpg123 ")) {
                        var split = msg.Split(" ");
                        if (split.Length >= 2) {
                            Mpg123(split[1..]);
                        }
                    }
                }
                catch (Exception ex) {
                    if (_configuration.Verbose)
                        Console.WriteLine(ex.ToString());
                    else
                        Console.WriteLine(ex);
                    //Environment.Exit(-2);
                }
        }

        private static void Mpg123(params string[] args) {
            using var p = new Process {
                StartInfo = {
                    UseShellExecute = false,
                    FileName = "mpg123",
                    CreateNoWindow = false,
                }
            };
            foreach (var arg in args) {
                p.StartInfo.ArgumentList.Add(arg);
            }
            p.Start();
            p.WaitForExit();
        }
    }
}