using System.Collections.Generic;
using System.Text;

namespace EaGpt
{
    public sealed class ModelSnapshot
    {
        public string Name { get; set; } = "";
        public List<SnapshotElement> Elements { get; } = new List<SnapshotElement>();
        public List<SnapshotRelationship> Relationships { get; } = new List<SnapshotRelationship>();
        public List<SnapshotDiagram> Diagrams { get; } = new List<SnapshotDiagram>();
    }

    public sealed class SnapshotElement
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public sealed class SnapshotRelationship
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public sealed class SnapshotDiagram
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Viewpoint { get; set; } = "";
        public List<SnapshotNode> Nodes { get; } = new List<SnapshotNode>();
    }

    public sealed class SnapshotNode
    {
        public string ElementId { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public static class ModelDigestBuilder
    {
        public const int DefaultMaxChars = 24000;

        public static string ToXml(ModelSnapshot snapshot, int maxChars = DefaultMaxChars)
        {
            var sb = new StringBuilder();
            sb.Append("<archimate name=\"").Append(XmlEscape(snapshot.Name)).Append("\">\n");
            sb.Append("  <elements>\n");
            foreach (var e in snapshot.Elements)
            {
                sb.Append("    <element id=\"").Append(XmlEscape(e.Id))
                    .Append("\" type=\"").Append(XmlEscape(e.Type))
                    .Append("\" name=\"").Append(XmlEscape(e.Name)).Append("\"/>\n");
            }

            sb.Append("  </elements>\n  <relationships>\n");
            foreach (var r in snapshot.Relationships)
            {
                sb.Append("    <relationship id=\"").Append(XmlEscape(r.Id))
                    .Append("\" type=\"").Append(XmlEscape(r.Type))
                    .Append("\" source=\"").Append(XmlEscape(r.Source))
                    .Append("\" target=\"").Append(XmlEscape(r.Target))
                    .Append("\" name=\"").Append(XmlEscape(r.Name)).Append("\"/>\n");
            }

            sb.Append("  </relationships>\n  <views>\n");
            foreach (var d in snapshot.Diagrams)
            {
                sb.Append("    <view id=\"").Append(XmlEscape(d.Id))
                    .Append("\" name=\"").Append(XmlEscape(d.Name))
                    .Append("\" viewpoint=\"").Append(XmlEscape(d.Viewpoint)).Append("\">\n");
                foreach (var n in d.Nodes)
                {
                    sb.Append("      <node elementId=\"").Append(XmlEscape(n.ElementId))
                        .Append("\" x=\"").Append(n.X)
                        .Append("\" y=\"").Append(n.Y)
                        .Append("\" width=\"").Append(n.Width)
                        .Append("\" height=\"").Append(n.Height).Append("\"/>\n");
                }

                sb.Append("    </view>\n");
            }

            sb.Append("  </views>\n</archimate>");
            string xml = sb.ToString();
            if (xml.Length <= maxChars)
            {
                return xml;
            }

            return xml.Substring(0, maxChars) + "\n<!-- truncated -->\n";
        }

        private static string XmlEscape(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            return s!.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }

    public static class SelectionContextFormatter
    {
        public static string Format(IEnumerable<string> lines)
        {
            var sb = new StringBuilder();
            bool any = false;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!any)
                {
                    sb.Append("Current selection in the model:\n");
                    any = true;
                }

                sb.Append("- ").Append(line).Append('\n');
            }

            return any ? sb.ToString() : "";
        }
    }
}
