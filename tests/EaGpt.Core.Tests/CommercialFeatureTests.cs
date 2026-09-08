using System.Collections.Generic;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class CommercialFeatureTests
    {
        private static ModelSnapshot Portfolio()
        {
            string crm = "id-" + new string('1', 32);
            string hire = "id-" + new string('2', 32);
            string ghost = "id-" + new string('3', 32);
            var snap = new ModelSnapshot { Name = "P" };
            snap.Elements.Add(new SnapshotElement { Id = crm, Type = "ApplicationComponent", Name = "CRM" });
            snap.Elements.Add(new SnapshotElement { Id = hire, Type = "BusinessProcess", Name = "Hire" });
            snap.Elements.Add(new SnapshotElement { Id = ghost, Type = "DataObject", Name = "OrphanData" });
            snap.Elements.Add(new SnapshotElement { Id = "id-" + new string('4', 32), Type = "ApplicationComponent", Name = "CRM" });
            snap.Relationships.Add(new SnapshotRelationship
            {
                Id = "id-" + new string('r', 32),
                Type = "AccessRelationship",
                Source = crm,
                Target = hire
            });
            snap.Diagrams.Add(new SnapshotDiagram { Name = "Apps" });
            snap.Diagrams[0].Nodes.Add(new SnapshotNode { ElementId = crm, X = 1, Y = 1, Width = 120, Height = 55 });
            return snap;
        }

        [Fact]
        public void Quality_SummaryAndAuditList()
        {
            var snap = Portfolio();
            string summary = ModelQuality.Format(snap, detailed: false);
            Assert.Contains("MODEL QUALITY:", summary);
            Assert.Contains("illegal existing relationship", summary);
            Assert.Contains("audit the model", summary);

            string detail = ModelQuality.Format(snap, detailed: true);
            Assert.Contains("OrphanData", detail);
            Assert.Contains("Illegal existing", detail);
            Assert.Contains("Hire", detail);
            Assert.Contains("Duplicate type+name", detail);
            Assert.Contains("Not on any view", detail);
            Assert.True(ModelQuality.LooksLikeAuditQuery("Please audit the model quality"));
        }

        [Fact]
        public void Search_FindsNamedComponent()
        {
            var snap = Portfolio();
            Assert.True(ModelSearch.LooksLikeSearchQuery("find CRM"));
            string hits = ModelSearch.Format(snap, "find CRM");
            Assert.Contains("SEARCH HITS", hits);
            Assert.Contains("CRM", hits);
        }

        [Fact]
        public void History_KeepsLastTurnsTrimmed()
        {
            var turns = new List<ChatTurn>();
            for (int i = 0; i < 6; i++)
            {
                ChatHistory.Remember(turns, "ask " + i, "answer " + i);
            }

            Assert.Equal(ChatHistory.MaxTurns, turns.Count);
            Assert.Equal("ask 2", turns[0].User);
            Assert.Contains("…", ChatHistory.Trim(new string('x', ChatHistory.MaxUserChars + 10), ChatHistory.MaxUserChars));
        }

        [Fact]
        public void AnalysisContext_IncludesQuality()
        {
            string ctx = ModelAnalysisContext.Build(Portfolio(), null, "audit the model");
            Assert.Contains("MODEL QUALITY:", ctx);
            Assert.Contains("OrphanData", ctx);
            Assert.Contains("SEARCH HITS", ModelAnalysisContext.Build(Portfolio(), null, "find CRM"));
        }

        [Fact]
        public void ChatRequest_IncludesPriorTurns()
        {
            var history = new List<ChatTurn>
            {
                new ChatTurn { User = "What is CRM?", Assistant = "An application component." }
            };
            string json = OllamaClient.BuildChatRequestJson("llama3.2", "sys", "Add a data object", stream: false, history);
            Assert.Contains("What is CRM?", json);
            Assert.Contains("An application component.", json);
            Assert.Contains("Add a data object", json);
            Assert.Contains("\"role\":\"assistant\"", json);
        }

        [Fact]
        public void ExtractChatDelta_OpenAiSseAndOllama()
        {
            Assert.Equal("Hi", OllamaJson.ExtractChatDelta("data: {\"choices\":[{\"delta\":{\"content\":\"Hi\"}}]}"));
            Assert.Equal("", OllamaJson.ExtractChatDelta("data: [DONE]"));
            Assert.Equal("Hello\nworld", OllamaJson.ExtractMessageContent("{\"message\":{\"role\":\"assistant\",\"content\":\"Hello\\nworld\"},\"done\":true}"));
        }

        [Fact]
        public void ParseOpenAiModelIds()
        {
            const string body = "{\"data\":[{\"id\":\"qwen2.5-7b\",\"object\":\"model\"},{\"id\":\"llama3.2\"}]}";
            var ids = OllamaJson.ParseOpenAiModelIds(body);
            Assert.Contains("qwen2.5-7b", ids);
            Assert.Contains("llama3.2", ids);
        }
    }
}
