using System;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class OllamaEndpointTests
    {
        [Theory]
        [InlineData("http://localhost:11434")]
        [InlineData("http://127.0.0.1:11434")]
        [InlineData("https://ollama.example.com")]
        [InlineData("192.168.1.10:11434")]
        [InlineData("localhost")]
        public void TryNormalize_AllowsLocalAndLan(string raw)
        {
            Assert.True(OllamaEndpoint.TryNormalize(raw, out string normalized, out string error), error);
            Assert.StartsWith("http", normalized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("@", normalized);
        }

        [Theory]
        [InlineData("file:///etc/passwd")]
        [InlineData("ftp://localhost/ollama")]
        [InlineData("http://user:secret@localhost:11434")]
        [InlineData("http://169.254.169.254/latest/meta-data")]
        [InlineData("http://metadata.google.internal/")]
        [InlineData("javascript:alert(1)")]
        public void TryNormalize_RejectsUnsafeUrls(string raw)
        {
            Assert.False(OllamaEndpoint.TryNormalize(raw, out _, out string error));
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void TryNormalize_StripsPathAndQuery()
        {
            Assert.True(OllamaEndpoint.TryNormalize("http://localhost:11434/api/chat?x=1", out string normalized, out _));
            Assert.Equal("http://localhost:11434", normalized);
        }

        [Fact]
        public void NormalizeOrDefault_FallsBack()
        {
            Assert.Equal(OllamaClient.DefaultBaseUrl, OllamaEndpoint.NormalizeOrDefault("file:///tmp"));
        }

        [Fact]
        public void Constructor_RejectsFileUrl()
        {
            Assert.Throws<ArgumentException>(() => new OllamaClient("file:///tmp", "llama3.2"));
        }

        [Fact]
        public void Constructor_AcceptsHostWithoutScheme()
        {
            var client = new OllamaClient("127.0.0.1:11434", "llama3.2");
            Assert.Equal("http://127.0.0.1:11434", client.BaseUrl);
            Assert.Equal("llama3.2", client.Model);
        }

        [Theory]
        [InlineData("http://169.254.169.254/")]
        [InlineData("http://2852039166/")]
        [InlineData("http://0xa9fea9fe/")]
        [InlineData("http://0251.0376.0251.0376/")]
        [InlineData("http://0xa9.0xfe.0xa9.0xfe/")]
        [InlineData("http://[::ffff:169.254.169.254]/")]
        [InlineData("http://[fd00:ec2::254]/")]
        [InlineData("http://metadata.google.internal./")]
        [InlineData("http://100.100.100.200/")]
        [InlineData("http://instance-data/")]
        public void TryNormalize_RejectsMetadataEncodings(string raw)
        {
            Assert.False(OllamaEndpoint.TryNormalize(raw, out _, out string error), raw);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void TryNormalize_RejectsOverlongUrl()
        {
            string raw = "http://localhost/" + new string('a', OllamaEndpoint.MaxUrlLength);
            Assert.False(OllamaEndpoint.TryNormalize(raw, out _, out _));
        }
    }
}
