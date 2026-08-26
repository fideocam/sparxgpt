using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class UserMessageAndDigestTests
    {
        [Fact]
        public void BuildUserMessage_PutsXmlFirst()
        {
            string msg = UserMessageBuilder.BuildUserMessage("Current selection in the model:\n- Element\n", "<archimate/>", "Add a Business Actor called Customer");
            Assert.StartsWith("ArchiMate model (compact XML):", msg);
            Assert.Contains("--- END OF MODEL ---", msg);
            Assert.Contains("User request: Add a Business Actor called Customer", msg);
            Assert.Contains("Current selection", msg);
        }

        [Fact]
        public void Digest_IncludesElementsAndViews()
        {
            var snap = new ModelSnapshot { Name = "Demo" };
            snap.Elements.Add(new SnapshotElement { Id = "id-aa", Type = "BusinessActor", Name = "Customer" });
            snap.Relationships.Add(new SnapshotRelationship
            {
                Id = "id-rel",
                Type = "ServingRelationship",
                Source = "id-aa",
                Target = "id-bb",
                Name = ""
            });
            snap.Diagrams.Add(new SnapshotDiagram { Id = "id-d", Name = "Idea", Viewpoint = "Business" });
            snap.Diagrams[0].Nodes.Add(new SnapshotNode { ElementId = "id-aa", X = 1, Y = 2, Width = 120, Height = 55 });

            string xml = ModelDigestBuilder.ToXml(snap);
            Assert.Contains("name=\"Demo\"", xml);
            Assert.Contains("type=\"BusinessActor\"", xml);
            Assert.Contains("name=\"Idea\"", xml);
            Assert.Contains("elementId=\"id-aa\"", xml);
        }

        [Fact]
        public void Digest_TruncatesWhenOverBudget()
        {
            var snap = new ModelSnapshot();
            for (int i = 0; i < 200; i++)
            {
                snap.Elements.Add(new SnapshotElement { Id = "id-" + i, Type = "BusinessActor", Name = "N" + i });
            }

            string xml = ModelDigestBuilder.ToXml(snap, maxChars: 500);
            Assert.True(xml.Length <= 530);
            Assert.Contains("truncated", xml);
        }

        [Fact]
        public void SelectionFormatter_EmptyWhenNoLines()
        {
            Assert.Equal("", SelectionContextFormatter.Format(new string[0]));
            Assert.Contains("Current selection", SelectionContextFormatter.Format(new[] { "Element BusinessActor \"Customer\" (id=id-aa)" }));
        }
    }
}
