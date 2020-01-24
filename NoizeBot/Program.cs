using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Jint;
using WebsocketClient;

namespace NoizeBot {
    internal class Program {
        private static CancellationToken _cancellationToken;
        private static Configuration _configuration;
        private static readonly Channel<PostedEvent> PostedChannel = Channel.CreateUnbounded<PostedEvent>();
        public static Action Shutdown;

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
            var processingTask = Process();
            var listenTask = socket.Listen();
            await Task.WhenAny(processingTask, listenTask);
        }

        private static async Task Process() {
            var featuresJs = GetEmbeddedResource("NoizeBot.features.js");
            await foreach (var e in PostedChannel.Reader.ReadAllAsync())
                try {
                    if(string.IsNullOrWhiteSpace(e?.Post?.Message)) continue;
                    var message = e.Post.Message.Trim();
                    var cmd = string.Empty;
                    var args = string.Empty;
                    var split = message.Split(" ", 2);
                    if (split != null && split.Length == 2) {
                        cmd = split[0];
                        args = split[1];
                    }

                    
                    var engine = new Engine();
                    engine.SetValue("log", new Action<string>(Console.WriteLine));
                    engine.SetValue("run", new Action<string, string[]>(Run));
                    engine.SetValue("exec", new Action<string, string>(Exec));
                    engine.SetValue("die", Shutdown);
                    engine.SetValue("message", message);
                    engine.SetValue("cmd", cmd);
                    engine.SetValue("args", args);
                    engine.SetValue("channel", e.ChannelDisplayName);
                    engine.SetValue("channelId", e.Post.ChannelId);
                    engine.Execute(featuresJs);
                }
                catch (Exception ex) {
                    if (_configuration.Verbose)
                        Console.WriteLine(ex.ToString());
                    else
                        Console.WriteLine(ex);
                    //Environment.Exit(-2);
                }
        }

        private static void Run(string command, string[] args) {
            using var p = new Process {
                StartInfo = {
                    UseShellExecute = false,
                    FileName = command,
                    CreateNoWindow = false,
                }
            };
            foreach (var arg in args) {
                p.StartInfo.ArgumentList.Add(arg);
            }
            p.Start();
            p.WaitForExit();
        }

        private static void Exec(string command, string args) {
            using var p = new Process {
                StartInfo = {
                    UseShellExecute = false,
                    FileName = command,
                    CreateNoWindow = false,
                    Arguments = args,
                }
            };
            p.Start();
            p.WaitForExit();
        }

        private static string GetEmbeddedResource(string name) {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            using var resource = assembly.GetManifestResourceStream(name);
            if (resource == null) return null;
            using var sr = new StreamReader(resource, Encoding.UTF8);
            return sr.ReadToEnd();
        }
    }
}