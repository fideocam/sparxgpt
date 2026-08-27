using System;
using System.Collections.Generic;
using System.Text;

namespace EaGpt
{
    public static class ModelSummary
    {
        public static string Format(ModelSnapshot snapshot)
        {
            var layers = new Dictionary<ArchimateLayer, int>();
            foreach (var e in snapshot.Elements)
            {
                var layer = ArchimateAspects.Classify(e.Type).Layer;
                if (!layers.ContainsKey(layer))
                {
                    layers[layer] = 0;
                }

                layers[layer]++;
            }

            var sb = new StringBuilder();
            sb.Append("MODEL SUMMARY: ").Append(snapshot.Elements.Count).Append(" element(s)");
            bool first = true;
            foreach (ArchimateLayer layer in new[]
                     {
                         ArchimateLayer.Motivation, ArchimateLayer.Strategy, ArchimateLayer.Business,
                         ArchimateLayer.Application, ArchimateLayer.Technology, ArchimateLayer.Implementation
                     })
            {
                if (!layers.TryGetValue(layer, out int n) || n == 0)
                {
                    continue;
                }

                sb.Append(first ? " (" : ", ");
                first = false;
                sb.Append(ArchimateAspects.LayerName(layer)).Append(' ').Append(n);
            }

            if (!first)
            {
                sb.Append(')');
            }

            sb.Append(", ").Append(snapshot.Relationships.Count).Append(" relationship(s), ")
                .Append(snapshot.Diagrams.Count).Append(" view(s)");
            if (snapshot.Diagrams.Count > 0)
            {
                sb.Append(" (");
                int shown = 0;
                foreach (var d in snapshot.Diagrams)
                {
                    if (shown >= 8)
                    {
                        sb.Append(", …");
                        break;
                    }

                    if (shown > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(d.Name);
                    shown++;
                }

                sb.Append(')');
            }

            sb.Append(".\n");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Deterministic analysis blocks sent with the user message so small Ollama models
    /// do not have to reconstruct counts, neighbors, or viewpoint rules from truncated XML.
    /// </summary>
    public static class ModelAnalysisContext
    {
        public static string Build(ModelSnapshot snapshot, string? selectionContext, string? prompt)
        {
            var sb = new StringBuilder();
            sb.Append(ModelSummary.Format(snapshot));

            var ids = SelectionIds.Parse(selectionContext);
            if (ids.Count == 0 && SelectionIds.LooksLikeImpactQuery(prompt))
            {
                ids.AddRange(SelectionIds.FindNamedElements(snapshot, prompt));
            }

            string impact = ImpactAnalyzer.Format(snapshot, ids);
            if (impact.Length > 0)
            {
                sb.Append(impact);
            }

            string mermaid = MermaidExporter.Neighborhood(snapshot, ids, selectionContext);
            if (mermaid.Length > 0)
            {
                sb.Append(mermaid);
            }

            sb.Append(ViewpointCatalog.FormatForPrompt(prompt));
            return sb.ToString();
        }
    }
}
