using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using Jint;
using MattermostApi;

namespace NoizeBot {
    public static class Processing {
        private static readonly string FeaturesJs = GetEmbeddedResource("NoizeBot.features.js");
        private static readonly string LibJs = GetEmbeddedResource("NoizeBot.lib.js");
        private static readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        private static readonly Configuration _configuration = new Configuration();
        public static void Process(string message, string channelDisplayName, string channelId, string rootPostId) {
            var reply = new Action<string>(msg => {
                if (_configuration.Repl) Console.WriteLine(msg);
                else CreatePost(msg, channelId, rootPostId);
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
                    botUserId = Program.BotUserId,
                    verbose = _configuration.Verbose,
                    repl = _configuration.Repl,
                    channel = channelDisplayName,
                    channelId,
                    postId = rootPostId,
                    ips = GetLocalAddressList()
                }.ToHumanReadableJson();
            });
            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Console.WriteLine));
            engine.SetValue("run", new Func<string, string[], int>(Run));
            engine.SetValue("tts", new Action<string>(Tts));
            engine.SetValue("playUrl", new Action<string>(PlayUrl));
            engine.SetValue("googleTts", new Action<string, string>(GoogleTts));
            engine.SetValue("reply", reply);
            engine.SetValue("die", Program.Shutdown);
            engine.SetValue("getStatusJson", getStatusJson);
            engine.SetValue("message", message);
            engine.SetValue("channel", channelDisplayName);
            engine.SetValue("verbose", _configuration.Verbose);
            engine.Execute(LibJs);
            engine.Execute(FeaturesJs);
            engine.Invoke("processMatches");
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
        public static Action KillRunningProcess;
        private static int Run(string command, string[] args = null) {
            if (_configuration.Verbose) Console.WriteLine(new {command, args}.ToHumanReadableJson());

            using var p = new Process {
                StartInfo = {
                    UseShellExecute = false,
                    FileName = command,
                    CreateNoWindow = false
                }
            };
            foreach (var arg in args ?? Array.Empty<string>()) {
                p.StartInfo.ArgumentList.Add(arg);
            }

            try {
                p.Start();
                KillRunningProcess = () => p.Kill(true);
                if (!p.WaitForExit(ProcessTimeoutMilliseconds)) KillRunningProcess();
                ;
            } finally {
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
            if (IsWin)
                TtsWin(msg);
            else // bsd and osx... good luck :)
                TtsLinux(msg);
        }
        private static void TtsLinux(string msg) {
            msg = msg?.Replace("\"", "\\\"") ?? "";
            Run("bash", new[] {"-c", $"echo \"{msg}\" | festival --tts "});
        }
        private static void TtsWin(string msg) {
            msg = msg?.Replace("'", "") ?? "";
            var args = new[] {
                "-Command",
                $"Add-Type –AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{msg}');"
            };
            Run("PowerShell", args);
        }
        private static void PlayUrl(string url) {
            if (string.IsNullOrWhiteSpace(url)) return;
            url = url.Replace("\"", "\\\"");
            ShellExec($"curl \"{url}\" | mpg123 -");
        }
        private static void ShellExec(string cmd) {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            var shell = IsWin ? "cmd" : "bash";
            var c = IsWin ? "/C" : "-c";
            var args = new[] {c, cmd};
            Run(shell, args);
        }
        private static void GoogleTts(string q, string tl) {
            // tl see: https://github.com/ncpierson/google-tts-languages/blob/master/src/index.js
            q = HttpUtility.UrlEncode(q?.Trim() ?? "");
            tl = HttpUtility.UrlEncode(tl?.Trim() ?? "");
            var url = $"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={q}&tl={tl}";
            Run("mpg123", new[] {url});
        }
        public static void CreatePost(string message, string channelId, string rootId = null) {
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
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        public static IEnumerable<string> GetLocalAddressList()
        {
            var hostname = Dns.GetHostName();
            yield return hostname;
            var host = Dns.GetHostEntry(hostname);
            foreach (var ip in host.AddressList)
            {
                yield return ip.ToString();
            }
        }
    }
}