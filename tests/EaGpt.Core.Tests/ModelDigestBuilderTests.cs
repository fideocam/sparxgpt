using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class ModelDigestBuilderTests
    {
        [Fact]
        public void Empty_model_is_valid_xml()
        {
            string xml = ModelDigestBuilder.ToXml(new ModelSnapshot { Name = "Empty" });
            Assert.Contains("<archimate name=\"Empty\">", xml);
            Assert.Contains("</archimate>", xml);
            Assert.Contains("<elements>", xml);
        }

        [Fact]
        public void Xml_escapes_element_names()
        {
            var snap = new ModelSnapshot { Name = "A & B" };
            snap.Elements.Add(new SnapshotElement
            {
                Id = "id-" + new string('a', 32),
                Type = "BusinessActor",
                Name = "A & B <C> \"quote\""
            });

            string xml = ModelDigestBuilder.ToXml(snap);
            Assert.Contains("name=\"A &amp; B\"", xml);
            Assert.Contains("A &amp; B &lt;C&gt; &quot;quote&quot;", xml);
            Assert.DoesNotContain("A & B <C>", xml);
        }

        [Fact]
        public void Xml_strips_control_characters_from_attributes()
        {
            var snap = new ModelSnapshot();
            snap.Elements.Add(new SnapshotElement
            {
                Id = "id-" + new string('b', 32),
                Type = "BusinessActor",
                Name = "evil\0name\nbreak\u0001"
            });

            string xml = ModelDigestBuilder.ToXml(snap);
            Assert.True(xml.IndexOf('\0') < 0);
            Assert.True(xml.IndexOf('\u0001') < 0);
            Assert.Contains("name=\"evilname break\"", xml);
        }

        [Fact]
        public void Relationships_and_diagrams_are_included()
        {
            string src = "id-" + new string('1', 32);
            string tgt = "id-" + new string('2', 32);
            var snap = new ModelSnapshot { Name = "Demo" };
            snap.Elements.Add(new SnapshotElement { Id = src, Name = "Src", Type = "BusinessActor" });
            snap.Elements.Add(new SnapshotElement { Id = tgt, Name = "Tgt", Type = "BusinessProcess" });
            snap.Relationships.Add(new SnapshotRelationship
            {
                Id = "id-" + new string('3', 32),
                Name = "uses",
                Type = "ServingRelationship",
                Source = src,
                Target = tgt
            });
            snap.Diagrams.Add(new SnapshotDiagram { Name = "Overview", Viewpoint = "Business" });
            snap.Diagrams[0].Nodes.Add(new SnapshotNode { ElementId = src, X = 10, Y = 20, Width = 120, Height = 55 });

            string xml = ModelDigestBuilder.ToXml(snap);
            Assert.Contains("type=\"ServingRelationship\"", xml);
            Assert.Contains("source=\"" + src + "\"", xml);
            Assert.Contains("name=\"Overview\"", xml);
            Assert.Contains("elementId=\"" + src + "\"", xml);
        }
    }
}
