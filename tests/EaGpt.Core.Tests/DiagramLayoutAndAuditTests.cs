using System.IO;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class DiagramLayoutAndAuditTests
    {
        [Fact]
        public void Layout_SpreadsCollapsedNodesByLayer()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec
            {
                Type = "BusinessActor",
                Name = "A",
                Id = "id-" + new string('1', 32)
            });
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec
            {
                Type = "ApplicationComponent",
                Name = "B",
                Id = "id-" + new string('2', 32)
            });
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "Landscape" };
            DiagramLayout.Prepare(result, snapshot: null);
            Assert.Equal(2, result.Diagram.Nodes.Count);
            Assert.NotEqual(result.Diagram.Nodes[0].Y, result.Diagram.Nodes[1].Y);
            Assert.True(result.Diagram.Nodes[0].Y < result.Diagram.Nodes[1].Y);
        }

        [Fact]
        public void Layout_LeavesExplicitCoordinates()
        {
            var result = new ArchiMateLlmResult();
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "Placed" };
            result.Diagram.Nodes.Add(new ArchiMateLlmResult.DiagramNodeSpec { ElementId = "a", X = 10, Y = 10, Width = 120, Height = 55 });
            result.Diagram.Nodes.Add(new ArchiMateLlmResult.DiagramNodeSpec { ElementId = "b", X = 400, Y = 80, Width = 120, Height = 55 });
            DiagramLayout.Prepare(result, snapshot: null);
            Assert.Equal(10, result.Diagram.Nodes[0].X);
            Assert.Equal(400, result.Diagram.Nodes[1].X);
        }

        [Fact]
        public void PreviewSummary_ListsAdds()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "BusinessActor", Name = "Customer", Id = "id-" + new string('a', 32) });
            result.Relationships.Add(new ArchiMateLlmResult.RelationshipSpec { Type = "ServingRelationship", Source = "a", Target = "b" });
            string preview = MutationPolicy.PreviewSummary(result);
            Assert.Contains("add 1 element", preview);
            Assert.Contains("Customer", preview);
            Assert.Contains("add 1 relationship", preview);
        }

        [Fact]
        public void AuditLog_WritesNdjson()
        {
            string path = Path.Combine(Path.GetTempPath(), "eagpt-audit-" + Path.GetRandomFileName() + ".ndjson");
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "BusinessActor", Name = "A", Id = "id-" + new string('a', 32) });
            AuditLog.TryAppend(path, result, "Add actor quoted", applied: true);
            string line = File.ReadAllText(path);
            Assert.Contains("\"applied\":true", line);
            Assert.Contains("\"elements\":1", line);
            Assert.Contains("Add actor quoted", line);
        }
    }
}
