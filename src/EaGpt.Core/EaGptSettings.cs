using System;
using System.IO;

namespace EaGpt
{
    public sealed class EaGptSettings
    {
        public string OllamaBaseUrl { get; set; } = OllamaClient.DefaultBaseUrl;
        public string Model { get; set; } = OllamaClient.DefaultModel;
        public int TimeoutMs { get; set; } = 180000;
        public string KnowledgeFolder { get; set; } = KnowledgeRetriever.DefaultFolder();
        public int KnowledgeMaxChars { get; set; } = KnowledgeRetriever.DefaultMaxChars;

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
                    settings.OllamaBaseUrl = OllamaEndpoint.NormalizeOrDefault(value);
                }
                else if (key.Equals("Model", StringComparison.OrdinalIgnoreCase))
                {
                    settings.Model = OllamaClient.SanitizeModelName(value);
                }
                else if (key.Equals("TimeoutMs", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int ms))
                {
                    settings.TimeoutMs = OllamaClient.ClampTimeout(ms);
                }
                else if (key.Equals("KnowledgeFolder", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
                {
                    settings.KnowledgeFolder = value;
                }
                else if (key.Equals("KnowledgeMaxChars", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int kc))
                {
                    settings.KnowledgeMaxChars = kc;
                }
            }

            return settings;
        }

        public void Save(string? path = null)
        {
            OllamaBaseUrl = OllamaEndpoint.NormalizeOrDefault(OllamaBaseUrl);
            Model = OllamaClient.SanitizeModelName(Model);
            TimeoutMs = OllamaClient.ClampTimeout(TimeoutMs);
            if (string.IsNullOrWhiteSpace(KnowledgeFolder))
            {
                KnowledgeFolder = KnowledgeRetriever.DefaultFolder();
            }

            if (KnowledgeMaxChars < 500)
            {
                KnowledgeMaxChars = 500;
            }

            if (KnowledgeMaxChars > 40_000)
            {
                KnowledgeMaxChars = 40_000;
            }
            string file = path ?? DefaultPath();
            string dir = Path.GetDirectoryName(file) ?? "";
            if (dir.Length > 0)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(file,
                "OllamaBaseUrl=" + OllamaBaseUrl + Environment.NewLine +
                "Model=" + Model + Environment.NewLine +
                "TimeoutMs=" + TimeoutMs + Environment.NewLine +
                "KnowledgeFolder=" + KnowledgeFolder + Environment.NewLine +
                "KnowledgeMaxChars=" + KnowledgeMaxChars + Environment.NewLine);
        }
    }
}
