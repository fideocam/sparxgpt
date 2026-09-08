using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class OllamaClientTests
    {
        [Fact]
        public void BuildChatRequestJson_EscapesAndIncludesSystem()
        {
            string json = OllamaClient.BuildChatRequestJson("llama3.2", "sys \"x\"", "user\nline", stream: false);
            Assert.Contains("\"model\":\"llama3.2\"", json);
            Assert.Contains("\"stream\":false", json);
            Assert.Contains("\"role\":\"system\"", json);
            Assert.Contains("sys \\\"x\\\"", json);
            Assert.Contains("user\\nline", json);
        }

        [Fact]
        public void BuildChatRequestJson_IncludesNumCtxWhenLargeEnough()
        {
            string json = OllamaClient.BuildChatRequestJson("llama3.2", "sys", "user", stream: false, history: null, numCtx: 32768);
            Assert.Contains("\"options\":{\"num_ctx\":32768}", json);
        }

        [Fact]
        public void BuildChatRequestJson_OmitsNumCtxWhenUnset()
        {
            string json = OllamaClient.BuildChatRequestJson("llama3.2", "sys", "user", stream: false, history: null, numCtx: 0);
            Assert.DoesNotContain("num_ctx", json);
        }

        [Fact]
        public void ParseShowContextTokens_FromModelInfo()
        {
            const string json = "{\"model_info\":{\"llama.context_length\":131072}}";
            Assert.Equal(131072, OllamaJson.ParseShowContextTokens(json));
        }

        [Fact]
        public void ParseShowContextTokens_PicksLargest()
        {
            const string json = "{\"model_info\":{\"a.context_length\":4096,\"b.context_length\":16384,\"general.num_ctx\":8192}}";
            Assert.Equal(16384, OllamaJson.ParseShowContextTokens(json));
        }

        [Fact]
        public void ParseShowContextTokens_EmptyIsZero()
        {
            Assert.Equal(0, OllamaJson.ParseShowContextTokens(""));
            Assert.Equal(0, OllamaJson.ParseShowContextTokens(null));
        }

        [Fact]
        public void ParseModelNames_FromTagsPayload()
        {
            const string body = "{\"models\":[{\"name\":\"llama3.2:latest\",\"size\":1},{\"name\":\"mistral\"}]}";
            var names = OllamaJson.ParseModelNames(body);
            Assert.Contains("llama3.2:latest", names);
            Assert.Contains("mistral", names);
        }

        [Fact]
        public void ExtractMessageContent_FromChatResponse()
        {
            const string body = "{\"message\":{\"role\":\"assistant\",\"content\":\"Hello\\nworld\"},\"done\":true}";
            Assert.Equal("Hello\nworld", OllamaJson.ExtractMessageContent(body));
        }

        [Fact]
        public void SystemPrompt_LoadsEmbeddedFile()
        {
            string prompt = ArchiMateSystemPrompt.GetSystemPrompt();
            Assert.Contains("CHANGES", prompt);
            Assert.Contains("BusinessActor", prompt);
            Assert.Contains("removeElementFromDiagramIds", prompt);
        }
    }
}
