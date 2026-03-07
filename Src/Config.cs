using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZenitharClient.Src
{
    /*internal class Config
    {
        private string path;
        private ConfigData data;

        private class ConfigData
        {
            [JsonPropertyName("GuildToken")]
            public string? GuildToken { get; set; }

            [JsonPropertyName("Endpoint")]
            public string? Endpoint { get; set; }
        }

        internal Config(string path)
        {
            this.path = path;

            // Ensure directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    data = JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
                }
                catch
                {
                    // If file is invalid, start with defaults
                    data = new ConfigData();
                }
            }
            else
            {
                data = new ConfigData();
            }
        }

        internal void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        internal Boolean IsValid()
        {
            return !string.IsNullOrWhiteSpace(GuildToken) && !string.IsNullOrWhiteSpace(Endpoint);
        }

        internal string? GuildToken
        {
            get => data.GuildToken;
            set => data.GuildToken = value;
        }

        internal string? Endpoint
        {
            get => data.Endpoint;
            set => data.Endpoint = value;
        }
    }*/
}
