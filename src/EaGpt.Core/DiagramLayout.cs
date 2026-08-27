using System;
using System.Collections.Generic;

namespace EaGpt
{
    /// <summary>
    /// Layer-banded grid layout when the LLM stacks every node at the same coordinate.
    /// MCP tools use ELK; this is a small local substitute that still produces readable EA diagrams.
    /// </summary>
    public static class DiagramLayout
    {
        public const int CellWidth = 160;
        public const int CellHeight = 100;
        public const int OriginX = 40;
        public const int OriginY = 40;

        public static void Prepare(ArchiMateLlmResult result, ModelSnapshot? snapshot)
        {
            if (result.Diagram == null)
            {
                return;
            }

            EnsureNodes(result);
            ApplyIfCollapsed(result, snapshot);
        }

        public static void EnsureNodes(ArchiMateLlmResult result)
        {
            if (result.Diagram == null)
            {
                return;
            }

            var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in result.Diagram.Nodes)
            {
                if (!string.IsNullOrWhiteSpace(n.ElementId))
                {
                    have.Add(n.ElementId!.Trim());
                }
            }

            foreach (var e in result.Elements)
            {
                if (string.IsNullOrWhiteSpace(e.Id))
                {
                    continue;
                }

                if (string.Equals(e.Type, "View", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(e.Type, "Diagram", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string id = e.Id!.Trim();
                if (have.Add(id))
                {
                    result.Diagram.Nodes.Add(new ArchiMateLlmResult.DiagramNodeSpec
                    {
                        ElementId = id,
                        X = 0,
                        Y = 0,
                        Width = 120,
                        Height = 55
                    });
                }
            }
        }

        public static void ApplyIfCollapsed(ArchiMateLlmResult result, ModelSnapshot? snapshot)
        {
            if (result.Diagram == null || result.Diagram.Nodes.Count == 0)
            {
                return;
            }

            var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (snapshot != null)
            {
                foreach (var e in snapshot.Elements)
                {
                    if (!string.IsNullOrEmpty(e.Id))
                    {
                        types[e.Id] = ArchiMateSchemaValidator.NormalizeElementType(e.Type);
                    }
                }
            }

            foreach (var e in result.Elements)
            {
                if (string.IsNullOrWhiteSpace(e.Id))
                {
                    continue;
                }

                string id = e.Id!.Trim();
                types[id] = ArchiMateSchemaValidator.NormalizeElementType(e.Type);
            }

            foreach (var n in result.Diagram.Nodes)
            {
                if (n.Width < 40)
                {
                    n.Width = 120;
                }

                if (n.Height < 20)
                {
                    n.Height = 55;
                }
            }

            if (result.Diagram.Nodes.Count < 2)
            {
                if (result.Diagram.Nodes[0].X == 0 && result.Diagram.Nodes[0].Y == 0)
                {
                    result.Diagram.Nodes[0].X = OriginX;
                    result.Diagram.Nodes[0].Y = OriginY;
                }

                return;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var n in result.Diagram.Nodes)
            {
                unique.Add(n.X + "," + n.Y);
            }

            if (unique.Count > 1)
            {
                return;
            }

            var columns = new int[8];
            foreach (var n in result.Diagram.Nodes)
            {
                types.TryGetValue(n.ElementId ?? "", out string? type);
                int row = ArchimateAspects.LayoutRow(ArchimateAspects.Classify(type).Layer);
                if (row < 0 || row >= columns.Length)
                {
                    row = columns.Length - 1;
                }

                int col = columns[row];
                columns[row] = col + 1;
                n.X = OriginX + col * CellWidth;
                n.Y = OriginY + row * CellHeight;
            }
        }
    }
}
