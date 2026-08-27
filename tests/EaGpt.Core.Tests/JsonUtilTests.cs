using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class JsonUtilTests
    {
        [Fact]
        public void Escape_QuotesAndNewlines()
        {
            Assert.Equal("a\\\"b\\nc", JsonUtil.Escape("a\"b\nc"));
        }

        [Fact]
        public void Escape_ControlCharactersAsUnicode()
        {
            Assert.Equal("x\\u0001y", JsonUtil.Escape("x\u0001y"));
        }

        [Fact]
        public void FindMatchingBracket_IgnoresBracesInStrings()
        {
            const string json = "{\"name\":\"a}b\",\"x\":{}}";
            int end = JsonUtil.FindMatchingBracket(json, 0);
            Assert.Equal(json.Length - 1, end);
        }

        [Fact]
        public void ReadJsonString_Unescapes()
        {
            Assert.Equal("Hello\nworld", JsonUtil.ReadJsonString("Hello\\nworld\"", 0));
        }
    }
}
