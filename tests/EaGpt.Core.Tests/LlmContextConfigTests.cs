using System.Collections.Generic;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class LlmContextConfigTests
    {
        [Fact]
        public void ResolveNumCtx_UseModelMaxSkips32kCap()
        {
            Assert.Equal(131072, LlmContextConfig.ResolveNumCtx(131072, 32768, useModelMax: true));
        }

        [Fact]
        public void ResolveNumCtx_CustomUiCappedToReported()
        {
            Assert.Equal(8192, LlmContextConfig.ResolveNumCtx(8192, 65536, useModelMax: false));
        }

        [Fact]
        public void ResolveNumCtx_CustomUiUsedWhenBelowReported()
        {
            Assert.Equal(65536, LlmContextConfig.ResolveNumCtx(131072, 65536, useModelMax: false));
        }

        [Fact]
        public void ResolveNumCtx_DefaultCapWhenNotUseModelMax()
        {
            Assert.Equal(LlmContextConfig.DefaultReportedCtxCap,
                LlmContextConfig.ResolveNumCtx(131072, 0, useModelMax: false));
        }

        [Fact]
        public void ResolveNumCtx_UseModelMaxFallsBackToUiWhenShowMissing()
        {
            Assert.Equal(65536, LlmContextConfig.ResolveNumCtx(0, 65536, useModelMax: true));
        }

        [Fact]
        public void ResolveReadTimeoutMs_ScalesWithLargeNumCtx()
        {
            Assert.Equal(480000, LlmContextConfig.ResolveReadTimeoutMs(131072, 0));
            Assert.Equal(LlmContextConfig.DefaultTimeoutMs, LlmContextConfig.ResolveReadTimeoutMs(32768, 0));
            Assert.Equal(240000, LlmContextConfig.ResolveReadTimeoutMs(65536, 0));
        }

        [Fact]
        public void ResolveMaxXmlChars_GrowsWithContext()
        {
            int small = LlmContextConfig.ResolveMaxXmlChars(8192, 2000, 500);
            int large = LlmContextConfig.ResolveMaxXmlChars(131072, 2000, 500);
            Assert.True(large > small);
            Assert.True(large <= LlmContextConfig.MaxXmlCharsCeiling);
        }
    }

    public class OllamaModelListTests
    {
        [Fact]
        public void FromServerNames_SortsAndDeduplicatesWithoutInventingDefault()
        {
            var items = OllamaModelList.FromServerNames(new[] { "mistral:latest", "  phi3  ", "mistral:latest", "" });
            Assert.Equal(new[] { "mistral:latest", "phi3" }, items);
            Assert.DoesNotContain(OllamaClient.DefaultModel, items);
        }

        [Fact]
        public void FromServerNames_EmptyWhenServerListsNothing()
        {
            Assert.Empty(OllamaModelList.FromServerNames(null));
            Assert.Empty(OllamaModelList.FromServerNames(new string[0]));
            Assert.Empty(OllamaModelList.FromServerNames(new[] { "  ", null! }));
        }

        [Fact]
        public void IndexOfModel_PrefersExactThenLatestSuffix()
        {
            var items = new List<string> { "llama3.2:latest", "mistral:latest" };
            Assert.Equal(0, OllamaModelList.IndexOfModel(items, "llama3.2"));
            Assert.Equal(0, OllamaModelList.IndexOfModel(items, "llama3.2:latest"));
            Assert.Equal(1, OllamaModelList.IndexOfModel(items, "mistral:latest"));
            Assert.Equal(-1, OllamaModelList.IndexOfModel(items, "phi3"));
        }

        [Fact]
        public void SelectionAfterRefresh_KeepsPreviousWhenServerListsIt()
        {
            var items = new[] { "phi3:latest", "mistral:latest" };
            Assert.Equal("mistral:latest", OllamaModelList.SelectionAfterRefresh(items, "mistral:latest"));
        }

        [Fact]
        public void SelectionAfterRefresh_MatchesBareNameToLatestTag()
        {
            var items = new[] { "llama3.2:latest", "mistral" };
            Assert.Equal("llama3.2:latest", OllamaModelList.SelectionAfterRefresh(items, "llama3.2"));
        }

        [Fact]
        public void SelectionAfterRefresh_FallsBackToFirstServerModelNotDefault()
        {
            var items = new[] { "mistral:latest", "phi3" };
            Assert.Equal("mistral:latest", OllamaModelList.SelectionAfterRefresh(items, OllamaClient.DefaultModel));
            Assert.NotEqual(OllamaClient.DefaultModel, OllamaModelList.SelectionAfterRefresh(items, "gone"));
        }

        [Fact]
        public void SelectionAfterRefresh_EmptyWhenServerHasNoModels()
        {
            Assert.Equal("", OllamaModelList.SelectionAfterRefresh(new string[0], "llama3.2"));
            Assert.Equal("", OllamaModelList.SelectionAfterRefresh(null, OllamaClient.DefaultModel));
        }
    }
}
