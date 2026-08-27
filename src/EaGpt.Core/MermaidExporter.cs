using System;
using System.Collections.Generic;
using System.Text;

namespace EaGpt
{
    /// <summary>
    /// Compact Mermaid neighborhood for the prompt (and optional export). Inspired by archimate-mcp's Mermaid export.
    /// </summary>
    public static class MermaidExporter
    {
        public const int DefaultMaxNodes = 24;

        public static string Neighborhood(ModelSnapshot snapshot, IList<string>? seedIds, string? selectionContext = null, int maxNodes = DefaultMaxNodes)
        {
            if (snapshot == null)
            {
                return "";
            }

            var seeds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (seedIds != null)
            {
                foreach (string id in seedIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        seeds.Add(id);
                    }
                }
            }

            if (seeds.Count == 0)
            {
                string? diagramName = SelectionIds.OpenDiagramName(selectionContext);
                if (!string.IsNullOrEmpty(diagramName))
                {
                    foreach (var d in snapshot.Diagrams)
                    {
                        if (string.Equals(d.Name, diagramName, StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var n in d.Nodes)
                            {
                                if (!string.IsNullOrEmpty(n.ElementId))
                                {
                                    seeds.Add(n.ElementId);
                                }
                            }

                            break;
                        }
                    }
                }
            }

            if (seeds.Count == 0)
            {
                return "";
            }

            var include = new HashSet<string>(seeds, StringComparer.OrdinalIgnoreCase);
            foreach (var r in snapshot.Relationships)
            {
                bool src = include.Contains(r.Source);
                bool tgt = include.Contains(r.Target);
                if (src && !string.IsNullOrEmpty(r.Target))
                {
                    include.Add(r.Target);
                }

                if (tgt && !string.IsNullOrEmpty(r.Source))
                {
                    include.Add(r.Source);
                }
            }

            var byId = new Dictionary<string, SnapshotElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in snapshot.Elements)
            {
                if (!string.IsNullOrEmpty(e.Id))
                {
                    byId[e.Id] = e;
                }
            }

            var nodes = new List<string>();
            foreach (string id in include)
            {
                if (byId.ContainsKey(id))
                {
                    nodes.Add(id);
                }
            }

            if (nodes.Count == 0)
            {
                return "";
            }

            if (maxNodes < 4)
            {
                maxNodes = 4;
            }

            if (nodes.Count > maxNodes)
            {
                nodes.RemoveRange(maxNodes, nodes.Count - maxNodes);
            }

            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder();
            sb.Append("NEIGHBORHOOD (Mermaid, 1 hop; use XML ids for mutations):\n```mermaid\nflowchart LR\n");
            for (int i = 0; i < nodes.Count; i++)
            {
                index[nodes[i]] = i;
                var e = byId[nodes[i]];
                sb.Append("  n").Append(i).Append("[\"").Append(Escape(e.Name)).Append("\\n").Append(Escape(e.Type)).Append("\"]\n");
            }

            foreach (var r in snapshot.Relationships)
            {
                if (!index.TryGetValue(r.Source, out int si) || !index.TryGetValue(r.Target, out int ti))
                {
                    continue;
                }

                string label = ShortRel(r.Type);
                sb.Append("  n").Append(si).Append(" -->|").Append(Escape(label)).Append("| n").Append(ti).Append('\n');
            }

            sb.Append("```\n");
            return sb.ToString();
        }

        public static string FromDiagram(ModelSnapshot snapshot, SnapshotDiagram diagram, int maxNodes = DefaultMaxNodes)
        {
            var ids = new List<string>();
            foreach (var n in diagram.Nodes)
            {
                if (!string.IsNullOrEmpty(n.ElementId))
                {
                    ids.Add(n.ElementId);
                }
            }

            return Neighborhood(snapshot, ids, null, maxNodes);
        }

        private static string ShortRel(string? type)
        {
            string t = ArchiMateSchemaValidator.NormalizeRelationshipType(type);
            if (t.EndsWith("Relationship", StringComparison.Ordinal))
            {
                t = t.Substring(0, t.Length - "Relationship".Length);
            }

            return t.Length == 0 ? "Association" : t;
        }

        private static string Escape(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            return s!.Replace("\\", "/").Replace("\"", "'").Replace("[", "(").Replace("]", ")").Replace("\n", " ").Replace("\r", " ");
        }
    }
}
