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
            Assert.False(ArchiMateLlmResultParser.LooksLikeChangesJson("There are several elements and relationships in this model."));
            Assert.True(ArchiMateLlmResultParser.LooksLikeChangesJson("{\"elements\":[],\"relationships\":[]}"));
        }

        [Fact]
        public void Parse_IgnoresUnknownKeys()
        {
            const string json = @"{
  ""elements"":[{""type"":""BusinessActor"",""name"":""Customer"",""id"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"",""extra"":""ignore""}],
  ""osCommand"":""rm -rf /"",
  ""relationships"":[]
}";
            var result = ArchiMateLlmResultParser.Parse(json);
            Assert.Single(result.Elements);
            Assert.Equal("Customer", result.Elements[0].Name);
            Assert.Empty(result.Relationships);
        }

        [Fact]
        public void Parse_OversizedReply_SetsError()
        {
            string raw = "{\"elements\":[" + new string('x', MutationPolicy.MaxReplyChars + 10) + "]}";
            var result = ArchiMateLlmResultParser.Parse(raw);
            Assert.Equal("Reply too large to apply as model changes.", result.Error);
            Assert.False(result.HasMutations);
            Assert.False(ArchiMateLlmResultParser.LooksLikeChangesJson(raw));
        }

        [Fact]
        public void Parse_ClampsHugeDiagramCoordinates()
        {
            const string json = @"{
  ""elements"":[],
  ""relationships"":[],
  ""diagram"":{
    ""name"":""Layout"",
    ""nodes"":[{""elementId"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"",""x"":999999,""y"":-40,""width"":12,""height"":9}]
  }
}";
            var result = ArchiMateLlmResultParser.Parse(json);
            Assert.NotNull(result.Diagram);
            Assert.Equal(MutationPolicy.MaxCoord, result.Diagram!.Nodes[0].X);
            Assert.Equal(0, result.Diagram.Nodes[0].Y);
        }

        [Fact]
        public void Parse_ErrorFieldWithoutMutations()
        {
            var result = ArchiMateLlmResultParser.Parse("{\"error\":\"I do not understand\"}");
            Assert.Equal("I do not understand", result.Error);
            Assert.False(result.HasMutations);
        }

        [Fact]
        public void Parse_EmptyOrNull_ReturnsEmptyResult()
        {
            Assert.Empty(ArchiMateLlmResultParser.Parse(null).Elements);
            Assert.Empty(ArchiMateLlmResultParser.Parse("").Elements);
            Assert.Empty(ArchiMateLlmResultParser.Parse("just plain text").Elements);
        }

        [Fact]
        public void Parse_RelationshipWithoutNameOrId()
        {
            const string json = @"{""elements"":[{""type"":""BusinessActor"",""name"":""A"",""id"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""}],""relationships"":[{""type"":""ServingRelationship"",""source"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"",""target"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""}]}";
            var result = ArchiMateLlmResultParser.Parse(json);
            Assert.Single(result.Relationships);
            Assert.Null(result.Relationships[0].Id);
            Assert.Equal("", result.Relationships[0].Name);
        }

        [Fact]
        public void Parse_DoesNotTreatDiagramWordInNameAsDiagramObject()
        {
            const string json = @"{""elements"":[{""type"":""BusinessActor"",""name"":""Some diagram"",""id"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""}],""relationships"":[]}";
            var result = ArchiMateLlmResultParser.Parse(json);
            Assert.Null(result.Diagram);
            Assert.Equal("Some diagram", result.Elements[0].Name);
        }

        [Fact]
        public void Parse_EscapedNewlineInName()
        {
            const string json = @"{""elements"":[{""type"":""BusinessActor"",""name"":""Line1\nLine2"",""id"":""id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""}],""relationships"":[]}";
            var result = ArchiMateLlmResultParser.Parse(json);
            Assert.Contains("\n", result.Elements[0].Name);
        }
    }
}
