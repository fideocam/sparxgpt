using System;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class OllamaClientHttpTests
    {
        [Fact]
        public void Chat_UnreachableLocalPort_Throws()
        {
            var client = new OllamaClient("http://127.0.0.1:1", "llama3.2", 3000);
            Assert.False(client.CheckConnection());
            Assert.ThrowsAny<Exception>(() => client.Chat("sys", "hello"));
        }

        [Fact]
        public void SanitizeModelName_DropsQuotesAndControls()
        {
            Assert.Equal(OllamaClient.DefaultModel, OllamaClient.SanitizeModelName("x\"y"));
            Assert.Equal(OllamaClient.DefaultModel, OllamaClient.SanitizeModelName("x\ny"));
            Assert.Equal("mistral:7b", OllamaClient.SanitizeModelName(" mistral:7b "));
        }

        [Fact]
        public void ClampTimeout_Bounds()
        {
            Assert.Equal(3000, OllamaClient.ClampTimeout(1));
            Assert.Equal(600000, OllamaClient.ClampTimeout(int.MaxValue));
            Assert.Equal(12000, OllamaClient.ClampTimeout(12000));
        }
    }
}
