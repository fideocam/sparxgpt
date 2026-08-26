using System;
using System.IO;

namespace EaGpt
{
    public sealed class EaGptSettings
    {
        public string OllamaBaseUrl { get; set; } = OllamaClient.DefaultBaseUrl;
        public string Model { get; set; } = OllamaClient.DefaultModel;
        public int TimeoutMs { get; set; } = 180000;

        public static string DefaultPath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(root))
            {
                root = Path.Combine(Path.GetTempPath(), "EaGpt");
            }

            return Path.Combine(root, "EaGpt", "settings.ini");
        }

        public static EaGptSettings Load(string? path = null)
        {
            var settings = new EaGptSettings();
            string file = path ?? DefaultPath();
            if (!File.Exists(file))
            {
                return settings;
            }

            foreach (string line in File.ReadAllLines(file))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Equals("OllamaBaseUrl", StringComparison.OrdinalIgnoreCase))
                {
                    settings.OllamaBaseUrl = value;
                }
                else if (key.Equals("Model", StringComparison.OrdinalIgnoreCase))
                {
                    settings.Model = value;
                }
                else if (key.Equals("TimeoutMs", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int ms))
                {
                    settings.TimeoutMs = ms;
                }
            }

            return settings;
        }

        public void Save(string? path = null)
        {
            string file = path ?? DefaultPath();
            string dir = Path.GetDirectoryName(file) ?? "";
            if (dir.Length > 0)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(file,
                "OllamaBaseUrl=" + OllamaBaseUrl + Environment.NewLine +
                "Model=" + Model + Environment.NewLine +
                "TimeoutMs=" + TimeoutMs + Environment.NewLine);
        }
    }
}
