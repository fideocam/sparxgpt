using System;
using System.IO;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void SaveAndLoad_RoundTrip()
        {
            string path = Path.Combine(Path.GetTempPath(), "eagpt-tests", Guid.NewGuid().ToString("N"), "settings.ini");
            var original = new EaGptSettings
            {
                OllamaBaseUrl = "http://192.168.1.8:11434",
                Model = "mistral",
                TimeoutMs = 45000
            };
            original.Save(path);
            EaGptSettings loaded = EaGptSettings.Load(path);
            Assert.Equal("http://192.168.1.8:11434", loaded.OllamaBaseUrl);
            Assert.Equal("mistral", loaded.Model);
            Assert.Equal(45000, loaded.TimeoutMs);
        }

        [Fact]
        public void Load_RejectsUnsafeUrlAndBadModel()
        {
            string path = Path.Combine(Path.GetTempPath(), "eagpt-tests", Guid.NewGuid().ToString("N"), "settings.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "OllamaBaseUrl=file:///tmp\nModel=bad\"model\nTimeoutMs=1\n");
            EaGptSettings loaded = EaGptSettings.Load(path);
            Assert.Equal(OllamaClient.DefaultBaseUrl, loaded.OllamaBaseUrl);
            Assert.Equal(OllamaClient.DefaultModel, loaded.Model);
            Assert.Equal(3000, loaded.TimeoutMs);
        }

        [Fact]
        public void Load_MissingFile_ReturnsDefaults()
        {
            EaGptSettings loaded = EaGptSettings.Load(Path.Combine(Path.GetTempPath(), "eagpt-missing-" + Guid.NewGuid().ToString("N") + ".ini"));
            Assert.Equal(OllamaClient.DefaultBaseUrl, loaded.OllamaBaseUrl);
            Assert.Equal(OllamaClient.DefaultModel, loaded.Model);
        }
    }
}
