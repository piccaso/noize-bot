using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        private static readonly string FeaturesJs = GetEmbeddedResource("NoizeBot.features.js");
        private static readonly string LibJs = GetEmbeddedResource("NoizeBot.lib.js");

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
            if (_configuration.Repl) {
                Repl();
                return;
            }
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
            
            Regex ignoreChannelsRegex = null;
            if (!string.IsNullOrWhiteSpace(_configuration.IgnoreChannelsRegex)) {
                ignoreChannelsRegex = new Regex(_configuration.IgnoreChannelsRegex);
            }
            await foreach (var e in PostedChannel.Reader.ReadAllAsync())
                try {
                    if(string.IsNullOrWhiteSpace(e?.Post?.Message)) continue;

                    if (ignoreChannelsRegex != null && !string.IsNullOrWhiteSpace(e.ChannelDisplayName)) {
                        if(ignoreChannelsRegex.Match(e.ChannelDisplayName).Success) continue;
                    }

                    var message = e.Post.Message.Trim();
                    Process(message, e.ChannelDisplayName, e.Post.ChannelId);

                }
                catch (Exception ex) {
                    if (_configuration.Verbose)
                        Console.WriteLine(ex.ToString());
                    else
                        Console.WriteLine(ex);
                    //Environment.Exit(-2);
                }
        }

        private static void Process(string message, string channelDisplayName, string channelId) {
            var cmd = string.Empty;
            var args = string.Empty;
            var split = message.Split(" ", 2);
            if (split != null && split.Length == 2)
            {
                cmd = split[0];
                args = split[1];
            }

            var engine = new Engine();
            engine.SetValue("log", new Action<string>(Console.WriteLine));
            engine.SetValue("run", new Action<string, string[]>(Run));
            engine.SetValue("exec", new Action<string, string>(Exec));
            engine.SetValue("tts", new Action<string>(Tts));
            engine.SetValue("playUrl", new Action<string>(PlayUrl));
            engine.SetValue("die", Shutdown);
            engine.SetValue("message", message);
            engine.SetValue("cmd", cmd);
            engine.SetValue("args", args);
            engine.SetValue("channel", channelDisplayName);
            engine.SetValue("channelId", channelId);
            engine.Execute(LibJs);
            engine.Execute(FeaturesJs);
        }

        private static void Repl() {
            Console.WriteLine($"Repl...\nEnter message:");
            while (!_cancellationToken.IsCancellationRequested) {
                Console.Write(">");
                var line = Console.ReadLine();
                if(line == null) break;
                Process(line, "channel", "asdf1234");
            }
        }

        private static string Json(object o, bool pretty = false) {
            return JsonSerializer.Serialize(o, new JsonSerializerOptions {
                WriteIndented = pretty
            });
        }

        private const int ProcessTimeoutMilliseconds = 1000 * 60 * 3;

        private static void Run(string command, string[] args = null) {

            if (_configuration.Verbose) {
                Console.WriteLine(Json(new {command, args}));
            }

            using var p = new Process {
                StartInfo = {
                    UseShellExecute = false,
                    FileName = command,
                    CreateNoWindow = false,
                }
            };
            foreach (var arg in args ?? Array.Empty<string>()) {
                p.StartInfo.ArgumentList.Add(arg);
            }
            p.Start();
            if (!p.WaitForExit(ProcessTimeoutMilliseconds)) {
                p.Kill(true);
            };
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
            if (!p.WaitForExit(ProcessTimeoutMilliseconds)) {
                p.Kill(true);
            };
        }

        private static string GetEmbeddedResource(string name) {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            using var resource = assembly.GetManifestResourceStream(name);
            if (resource == null) return null;
            using var sr = new StreamReader(resource, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        private static readonly bool IsWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static void Tts(string msg) {
            if (IsWin) {
                TtsWin(msg);
            }
            else {
                // bsd and osx... good luck :)
                TtsLinux(msg);
            }
        }

        private static void TtsLinux(string msg) {
            msg = msg?.Replace("\"", "\\\"") ?? "";
            Run("bash", new[] {"-c", $"echo \"{msg}\" | festival --tts "});
        }

        private static void TtsWin(string msg) {
            msg = msg?.Replace("'","") ?? "";
            var args = new[] {
                "-Command",
                $"Add-Type –AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{msg}');"
            };
            Run("PowerShell", args);
        }

        private static void PlayUrl(string url) {
            if(string.IsNullOrWhiteSpace(url)) return;
            ShellExec($"curl \"{url}\" | mpg123 -");
        }

        private static void ShellExec(string cmd) {
            if(string.IsNullOrWhiteSpace(cmd)) return;
            var shell = IsWin ? "cmd" : "bash";
            var c = IsWin ? "/C" : "-c";
            var args = new[] { c, cmd };
            Run(shell, args);
        }
    }
}