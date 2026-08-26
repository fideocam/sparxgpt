using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class ParserTests
    {
        [Fact]
        public void Parse_ElementsAndRelationships()
        {
            const string json = @"{
  ""elements"":[{""type"":""BusinessActor"",""name"":""Customer"",""id"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""}],
  ""relationships"":[{""type"":""ServingRelationship"",""source"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"",""target"":""id-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"",""name"":""uses"",""id"":""id-cccccccccccccccccccccccccccccccc""}]
}";
            var result = ArchiMateLlmResultParser.Parse(json);
            Assert.Single(result.Elements);
            Assert.Equal("BusinessActor", result.Elements[0].Type);
            Assert.Equal("Customer", result.Elements[0].Name);
            Assert.Single(result.Relationships);
            Assert.Equal("ServingRelationship", result.Relationships[0].Type);
            Assert.Equal("uses", result.Relationships[0].Name);
        }

        [Fact]
        public void Parse_ExtractsJsonFromProse()
        {
            string raw = "Here you go\n```json\n{\"elements\":[{\"type\":\"Node\",\"name\":\"Host\",\"id\":\"id-11111111111111111111111111111111\"}],\"relationships\":[]}\n```";
            var result = ArchiMateLlmResultParser.Parse(raw);
            Assert.Single(result.Elements);
            Assert.Equal("Host", result.Elements[0].Name);
        }

        [Fact]
        public void Parse_DiagramAndRemovals()
        {
            const string json = @"{
  ""elements"":[],
  ""relationships"":[],
  ""diagram"":{
    ""name"":""Recruitment Process"",
    ""viewpoint"":""Business"",
    ""nodes"":[{""elementId"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"",""x"":10,""y"":20,""width"":130,""height"":60}],
    ""connections"":[{""sourceElementId"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"",""targetElementId"":""id-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"",""relationshipId"":""id-cccccccccccccccccccccccccccccccc""}]
  },
  ""removeElementIds"":[""id-dead""],
  ""removeElementFromDiagramIds"":[""id-beef""],
  ""removeDiagramNames"":[""Idea""]
}";
            var result = ArchiMateLlmResultParser.Parse(json);
            Assert.NotNull(result.Diagram);
            Assert.Equal("Recruitment Process", result.Diagram!.Name);
            Assert.Single(result.Diagram.Nodes);
            Assert.Equal(10, result.Diagram.Nodes[0].X);
            Assert.Single(result.Diagram.Connections);
            Assert.Contains("id-dead", result.RemoveElementIds);
            Assert.Contains("id-beef", result.RemoveElementFromDiagramIds);
            Assert.Contains("Idea", result.RemoveDiagramNames);
            Assert.True(result.HasMutations);
        }

        [Fact]
        public void LooksLikeChangesJson_FalseForAnalysis()
        {
            Assert.False(ArchiMateLlmResultParser.LooksLikeChangesJson("The model contains a Customer actor."));
            Assert.True(ArchiMateLlmResultParser.LooksLikeChangesJson("{\"elements\":[],\"relationships\":[]}"));
        }
    }
}
