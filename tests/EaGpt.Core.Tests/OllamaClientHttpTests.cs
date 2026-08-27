using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class OllamaClientHttpTests
    {
        [Fact]
        public void Chat_ReadsNonStreamingMessage()
        {
            int port = GetFreePort();
            string prefix = "http://127.0.0.1:" + port + "/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            try
            {
                var server = listener.GetContextAsync();
                var client = new OllamaClient("http://127.0.0.1:" + port, "llama3.2", 8000);
                var chat = System.Threading.Tasks.Task.Run(() => client.Chat("sys", "hello"));
                HttpListenerContext ctx = server.GetAwaiter().GetResult();
                Assert.Equal("/api/chat", ctx.Request.Url!.AbsolutePath);
                byte[] payload = Encoding.UTF8.GetBytes("{\"message\":{\"role\":\"assistant\",\"content\":\"ok\"},\"done\":true}");
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.OutputStream.Write(payload, 0, payload.Length);
                ctx.Response.Close();
                Assert.Equal("ok", chat.GetAwaiter().GetResult());
            }
            finally
            {
                listener.Stop();
            }
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

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
