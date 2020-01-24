using System;
using Microsoft.Extensions.Configuration;

namespace NoizeBot {
    public class Configuration {
        private readonly IConfiguration _config;

        public Configuration() {
            _config = new ConfigurationBuilder()
                .AddUserSecrets<Configuration>()
                .AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .Build();
            bool.TryParse(_config["Verbose"], out Verbose);
        }

        private string this[string key] => _config[key];
        public string WebsocketUrl => this[nameof(WebsocketUrl)];
        public string Token => this[nameof(Token)];
        public string IgnoreChannelsRegex => this[nameof(IgnoreChannelsRegex)];
        public readonly bool Verbose;
    }
}