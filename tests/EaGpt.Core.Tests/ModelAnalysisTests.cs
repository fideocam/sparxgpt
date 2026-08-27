using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class ModelAnalysisTests
    {
        private static ModelSnapshot Demo()
        {
            string actor = "id-" + new string('a', 32);
            string proc = "id-" + new string('b', 32);
            var snap = new ModelSnapshot { Name = "Demo" };
            snap.Elements.Add(new SnapshotElement { Id = actor, Type = "BusinessActor", Name = "Customer" });
            snap.Elements.Add(new SnapshotElement { Id = proc, Type = "BusinessProcess", Name = "Hire" });
            snap.Elements.Add(new SnapshotElement { Id = "id-" + new string('c', 32), Type = "ApplicationComponent", Name = "CRM" });
            snap.Relationships.Add(new SnapshotRelationship
            {
                Id = "id-" + new string('r', 32),
                Type = "AssignmentRelationship",
                Source = actor,
                Target = proc,
                Name = ""
            });
            snap.Diagrams.Add(new SnapshotDiagram { Id = "id-" + new string('d', 32), Name = "Idea", Viewpoint = "Business" });
            snap.Diagrams[0].Nodes.Add(new SnapshotNode { ElementId = actor, X = 10, Y = 20, Width = 120, Height = 55 });
            snap.Diagrams[0].Nodes.Add(new SnapshotNode { ElementId = proc, X = 200, Y = 20, Width = 120, Height = 55 });
            return snap;
        }

        [Fact]
        public void SelectionIds_ParseFromContext()
        {
            string actor = "id-" + new string('a', 32);
            string ctx = "Current selection in the model:\n- Element BusinessActor \"Customer\" (id=" + actor + ") on diagram \"Idea\"\n- Primary diagram (open in editor) \"Idea\"\n";
            var ids = SelectionIds.Parse(ctx);
            Assert.Equal(new[] { actor }, ids);
            Assert.Equal("Idea", SelectionIds.OpenDiagramName(ctx));
        }

        [Fact]
        public void Impact_ListsOutgoingRelationship()
        {
            var snap = Demo();
            string actor = snap.Elements[0].Id;
            string text = ImpactAnalyzer.Format(snap, new[] { actor });
            Assert.Contains("IMPACT ANALYSIS", text);
            Assert.Contains("Customer", text);
            Assert.Contains("outgoing", text);
            Assert.Contains("Hire", text);
            Assert.Contains("1 relationship", text);
        }

        [Fact]
        public void Mermaid_IncludesNeighborhood()
        {
            var snap = Demo();
            string mermaid = MermaidExporter.Neighborhood(snap, new[] { snap.Elements[0].Id });
            Assert.Contains("```mermaid", mermaid);
            Assert.Contains("Customer", mermaid);
            Assert.Contains("Hire", mermaid);
            Assert.Contains("Assignment", mermaid);
        }

        [Fact]
        public void Mermaid_UsesOpenDiagramWhenNoSelection()
        {
            var snap = Demo();
            string mermaid = MermaidExporter.Neighborhood(snap, new string[0], "Primary diagram (open in editor) \"Idea\"");
            Assert.Contains("Customer", mermaid);
            Assert.Contains("Hire", mermaid);
        }

        [Fact]
        public void Summary_CountsLayers()
        {
            string text = ModelSummary.Format(Demo());
            Assert.Contains("3 element", text);
            Assert.Contains("Business 2", text);
            Assert.Contains("Application 1", text);
            Assert.Contains("Idea", text);
        }

        [Fact]
        public void Viewpoint_MatchesBusinessPrompt()
        {
            var recipe = ViewpointCatalog.Match("Create a business layer view of hiring");
            Assert.NotNull(recipe);
            Assert.Equal("business", recipe!.Name);
            string formatted = ViewpointCatalog.FormatForPrompt("deployment of CRM");
            Assert.Contains("VIEWPOINT RECIPE (technology)", formatted);
        }

        [Fact]
        public void AnalysisContext_AndUserMessage_Order()
        {
            var snap = Demo();
            string ctx = "Current selection in the model:\n- Element BusinessActor \"Customer\" (id=" + snap.Elements[0].Id + ")\n";
            string analysis = ModelAnalysisContext.Build(snap, ctx, "What depends on Customer? Also sketch a business view.");
            Assert.Contains("MODEL SUMMARY", analysis);
            Assert.Contains("IMPACT ANALYSIS", analysis);
            Assert.Contains("```mermaid", analysis);
            Assert.Contains("VIEWPOINT RECIPE (business)", analysis);

            string msg = UserMessageBuilder.BuildUserMessage(ctx, "<archimate/>", "impact of Customer", "KNOWLEDGE", analysis);
            Assert.True(msg.IndexOf("MODEL SUMMARY") > msg.IndexOf("--- END OF MODEL ---"));
            Assert.True(msg.IndexOf("KNOWLEDGE") > msg.IndexOf("MODEL SUMMARY"));
            Assert.True(msg.IndexOf("User request:") > msg.IndexOf("KNOWLEDGE"));
        }

        [Fact]
        public void FindNamedElements_OnImpactQuery()
        {
            var snap = Demo();
            Assert.True(SelectionIds.LooksLikeImpactQuery("what depends on Customer"));
            var ids = SelectionIds.FindNamedElements(snap, "impact of Customer please");
            Assert.Contains(snap.Elements[0].Id, ids);
        }
    }
}
