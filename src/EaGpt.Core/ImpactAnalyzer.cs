using System;
using System.Collections.Generic;
using System.Text;

namespace EaGpt
{
    /// <summary>
    /// Deterministic 1-hop impact walk over the snapshot.
    /// MCP servers expose this as a query tool so the LLM does not invent neighbors from truncated XML.
    /// </summary>
    public static class ImpactAnalyzer
    {
        public const int DefaultMaxLines = 24;

        public static string Format(ModelSnapshot snapshot, IList<string> seedIds, int maxLines = DefaultMaxLines)
        {
            if (snapshot == null || seedIds == null || seedIds.Count == 0)
            {
                return "";
            }

            var byId = new Dictionary<string, SnapshotElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in snapshot.Elements)
            {
                if (!string.IsNullOrEmpty(e.Id))
                {
                    byId[e.Id] = e;
                }
            }

            var seeds = new List<string>();
            var seenSeed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in seedIds)
            {
                if (!string.IsNullOrEmpty(id) && byId.ContainsKey(id) && seenSeed.Add(id))
                {
                    seeds.Add(id);
                }
            }

            if (seeds.Count == 0)
            {
                return "";
            }

            if (maxLines < 4)
            {
                maxLines = 4;
            }

            var sb = new StringBuilder();
            sb.Append("IMPACT ANALYSIS (1 hop from selection):\n");
            int lines = 0;
            int relsTouching = 0;

            foreach (string seed in seeds)
            {
                if (lines >= maxLines)
                {
                    sb.Append("- … truncated\n");
                    break;
                }

                var el = byId[seed];
                sb.Append("- ").Append(el.Type).Append(" \"").Append(el.Name).Append("\" (").Append(el.Id).Append(")\n");
                lines++;

                foreach (var r in snapshot.Relationships)
                {
                    bool outgoing = string.Equals(r.Source, seed, StringComparison.OrdinalIgnoreCase);
                    bool incoming = string.Equals(r.Target, seed, StringComparison.OrdinalIgnoreCase);
                    if (!outgoing && !incoming)
                    {
                        continue;
                    }

                    relsTouching++;
                    if (lines >= maxLines)
                    {
                        continue;
                    }

                    string otherId = outgoing ? r.Target : r.Source;
                    string otherName = otherId;
                    string otherType = "?";
                    if (byId.TryGetValue(otherId, out SnapshotElement? other) && other != null)
                    {
                        otherName = other.Name;
                        otherType = other.Type;
                    }

                    sb.Append("  ").Append(outgoing ? "outgoing" : "incoming").Append(": ").Append(r.Type)
                        .Append(outgoing ? " → " : " ← ")
                        .Append(otherType).Append(" \"").Append(otherName).Append("\"\n");
                    lines++;
                }
            }

            sb.Append("- Deleting the selected element(s) would drop ").Append(relsTouching)
                .Append(" relationship(s) that touch them.\n");
            return sb.ToString();
        }
    }
}
