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
using System.Web;
using Jint;
using MattermostApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebsocketClient;
using Channel = System.Threading.Channels.Channel;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NoizeBot {
    internal class Program {
        private static CancellationToken _cancellationToken;
        private static Configuration _configuration;
        private static readonly Channel<PostedEvent> PostedChannel = Channel.CreateUnbounded<PostedEvent>();
        public static Action Shutdown;
        private static readonly string FeaturesJs = GetEmbeddedResource("NoizeBot.features.js");
        private static readonly string LibJs = GetEmbeddedResource("NoizeBot.lib.js");
        private static readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;
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
            }
            catch (Exception e) {
                if (_cancellationToken.IsCancellationRequested) {
                    Console.WriteLine(e.Message);
                    Environment.Exit(-1);
                }

                throw;
            }
        }

        private static void KillMe() {
            try { Shutdown?.Invoke(); } catch {/*nah...*/}
            try { KillRunningProcess?.Invoke(); } catch {/*nah...*/}
            try { System.Diagnostics.Process.GetCurrentProcess().Kill(); } catch {/*nah...*/}
            try { Environment.Exit(99); } catch {/*nah...*/}
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
                if(BotUserId == e.Post.UserId) return;
                if (e.Post.Message == "nb_kill") {
                    try {
                        CreatePost(":dizzy_face:", e.Post.ChannelId, e.Post.Id);
                    }
                    finally {
                        KillMe();
                    }
                };
                PostedChannel.Writer.WriteAsync(e, _cancellationToken).GetAwaiter().GetResult();
            };
            socket.OnHello += sv => { Console.WriteLine($"ServerVersion: {sv}"); };
            socket.OnBotUserId += id => { BotUserId = id; };
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
                    Process(message, e.ChannelDisplayName, e.Post.ChannelId, e.Post.Id);

                }
                catch (Exception ex) {
                    if (_configuration.Verbose)
                        Console.WriteLine(ex.ToString());
                    else
                        Console.WriteLine(ex);
                    //Environment.Exit(-2);
                }
        }

        private static void Process(string message, string channelDisplayName, string channelId, string postId) {
            var reply = new Action<string>(msg => {
                if (_configuration.Repl) Console.WriteLine(msg);
                else CreatePost(msg, channelId, postId);
            });

            var getStatusJson = new Func<string>(delegate {
                var cp = System.Diagnostics.Process.GetCurrentProcess();
                var cpuTime = cp.TotalProcessorTime;
                var workingSet = FormatBytes(cp.WorkingSet64);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                var mem = GC.GetTotalAllocatedBytes(true);
                return new {
                    currentTime = DateTimeOffset.UtcNow,
                    startTime = StartTime,
                    upTime = DateTimeOffset.UtcNow - StartTime,
                    cpuTime,
                    memAllocated = FormatBytes(mem),
                    workingSet,
                    botUserId = BotUserId,
                    verbose = _configuration.Verbose,
                    repl = _configuration.Repl,
                    channel = channelDisplayName,
                    channelId,
                    postId,
                }.ToHumanReadableJson();
            });
            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Console.WriteLine));
            engine.SetValue("run", new Func<string, string[], int>(Run));
            engine.SetValue("tts", new Action<string>(Tts));
            engine.SetValue("playUrl", new Action<string>(PlayUrl));
            engine.SetValue("googleTts", new Action<string, string>(GoogleTts));
            engine.SetValue("reply", reply);
            engine.SetValue("die", Shutdown);
            engine.SetValue(name: "getStatusJson", getStatusJson);
            engine.SetValue("message", message);
            engine.SetValue("channel", channelDisplayName);
            engine.SetValue("verbose", _configuration.Verbose);
            engine.Execute(LibJs);
            engine.Execute(FeaturesJs);
            engine.Invoke("processMatches");
        }

        private static void Repl() {
            Console.WriteLine($"Repl...\nEnter message:");
            while (!_cancellationToken.IsCancellationRequested) {
                Console.Write(">");
                var line = Console.ReadLine();
                if(line == null) break;
                Process(line, "channel", "", "");
            }
        }

        private static string Json(object o, bool pretty = false) {
            return JsonSerializer.Serialize(o, new JsonSerializerOptions {
                WriteIndented = pretty
            });
        }

        private static string FormatBytes(long bytes) {
            var len = Convert.ToDecimal(bytes);
            string[] sizes = {"B", "KB", "MB", "GB", "TB"};
            var order = 0;
            while (len >= 1024 && order < sizes.Length - 1) {
                order++;
                len /= 1024;
            }

            return $"{len:0.###} {sizes[order]}";
        }

        private const int ProcessTimeoutMilliseconds = 1000 * 60 * 5;

        public static Action KillRunningProcess = null;

        private static int Run(string command, string[] args = null) {
            if (_configuration.Verbose) Console.WriteLine(Json(new {command, args}));

            using var p = new Process {
                StartInfo = {
                    UseShellExecute = false,
                    FileName = command,
                    CreateNoWindow = false
                }
            };
            foreach (var arg in args ?? Array.Empty<string>()) p.StartInfo.ArgumentList.Add(arg);

            try {
                p.Start();
                KillRunningProcess = () => p.Kill(true);
                if (!p.WaitForExit(ProcessTimeoutMilliseconds)) KillRunningProcess();
                ;
            }
            finally {
                KillRunningProcess = null;
            }


            return p.ExitCode;
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
            url = url.Replace("\"", "\\\"");
            ShellExec($"curl \"{url}\" | mpg123 -");
        }

        private static void ShellExec(string cmd) {
            if(string.IsNullOrWhiteSpace(cmd)) return;
            var shell = IsWin ? "cmd" : "bash";
            var c = IsWin ? "/C" : "-c";
            var args = new[] { c, cmd };
            Run(shell, args);
        }

        private static void GoogleTts(string q, string tl) {
            // tl see: https://github.com/ncpierson/google-tts-languages/blob/master/src/index.js
            q = HttpUtility.UrlEncode(q?.Trim() ?? "");
            tl = HttpUtility.UrlEncode(tl?.Trim() ?? "");
            var url = $"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={q}&tl={tl}";
            Run("mpg123", new []{url});
        }

        private static void CreatePost(string message, string channelId, string rootId = null) {
            try {
                if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(channelId)) return;
                var settings = new Settings {
                    AccessToken = _configuration.Token,
                    ServerUri = new Uri(_configuration.ServerUri),
                    TokenExpires = DateTime.Now.AddYears(1), // or maybe never

                    // required by the library but not used
                    ApplicationName = "NoizeBot",
                    RedirectUri = new Uri(_configuration.ServerUri)
                };
                using var api = new Api(settings);
                Post.Create(api, channelId, message, rootId).GetAwaiter().GetResult();
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}