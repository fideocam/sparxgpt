using System;
using System.Collections.Generic;
using System.Text;

namespace EaGpt
{
    /// <summary>
    /// Deterministic repository health checks. OneRAI, Kernaro Assist, and AI Power Tools
    /// all sell this as "audit / governance / orphans"; EaGPT computes it in C# so a small
    /// local model does not have to invent the findings from truncated XML.
    /// </summary>
    public static class ModelQuality
    {
        public const int FanThreshold = 10;
        public const int MaxDetailLines = 28;

        public static bool LooksLikeAuditQuery(string? prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return false;
            }

            string p = prompt!.ToLowerInvariant();
            return p.IndexOf("audit", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("quality", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("orphan", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("governance", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("conformance", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("inconsist", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("gap", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("laatu", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("puutte", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("orpo", StringComparison.Ordinal) >= 0;
        }

        public static string Format(ModelSnapshot snapshot, bool detailed)
        {
            var byId = new Dictionary<string, SnapshotElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in snapshot.Elements)
            {
                if (!string.IsNullOrEmpty(e.Id))
                {
                    byId[e.Id] = e;
                }
            }

            var incoming = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var outgoing = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var illegal = new List<string>();
            foreach (var r in snapshot.Relationships)
            {
                Bump(outgoing, r.Source);
                Bump(incoming, r.Target);
                if (!byId.TryGetValue(r.Source ?? "", out var src) ||
                    !byId.TryGetValue(r.Target ?? "", out var tgt) ||
                    src == null || tgt == null)
                {
                    continue;
                }

                if (!RelationshipLegality.IsAllowed(src.Type, r.Type, tgt.Type, out string reason))
                {
                    illegal.Add(src.Type + " \"" + src.Name + "\" -" + ShortRel(r.Type) + "-> " +
                                tgt.Type + " \"" + tgt.Name + "\" (" + reason + ")");
                }
            }

            var onView = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int emptyViews = 0;
            foreach (var d in snapshot.Diagrams)
            {
                if (d.Nodes.Count == 0)
                {
                    emptyViews++;
                }

                foreach (var n in d.Nodes)
                {
                    if (!string.IsNullOrEmpty(n.ElementId))
                    {
                        onView.Add(n.ElementId);
                    }
                }
            }

            var orphans = new List<string>();
            var missingView = new List<string>();
            var highFan = new List<string>();
            var names = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in snapshot.Elements)
            {
                int inn = Count(incoming, e.Id);
                int outn = Count(outgoing, e.Id);
                if (inn == 0 && outn == 0)
                {
                    orphans.Add(Label(e));
                }

                if (!onView.Contains(e.Id) && snapshot.Diagrams.Count > 0)
                {
                    missingView.Add(Label(e));
                }

                if (inn >= FanThreshold || outn >= FanThreshold)
                {
                    highFan.Add(Label(e) + " fan-in=" + inn + " fan-out=" + outn);
                }

                string key = (e.Type ?? "") + "\0" + (e.Name ?? "");
                if (!names.ContainsKey(key))
                {
                    names[key] = 0;
                }

                names[key]++;
                if (names[key] == 2)
                {
                    duplicateKeys.Add(e.Type + " \"" + e.Name + "\"");
                }
            }

            var sb = new StringBuilder();
            sb.Append("MODEL QUALITY: ")
                .Append(orphans.Count).Append(" with no relationships, ")
                .Append(missingView.Count).Append(" not on any view, ")
                .Append(illegal.Count).Append(" illegal existing relationship(s), ")
                .Append(duplicateKeys.Count).Append(" duplicate name(s), ")
                .Append(highFan.Count).Append(" high fan-in/out, ")
                .Append(emptyViews).Append(" empty view(s).");
            if (!detailed)
            {
                sb.Append(" Ask \"audit the model\" for the list.\n");
                return sb.ToString();
            }

            sb.Append('\n');
            AppendSection(sb, "No relationships", orphans);
            AppendSection(sb, "Not on any view", missingView);
            AppendSection(sb, "Illegal existing relationships", illegal);
            var dupes = new List<string>(duplicateKeys);
            dupes.Sort(StringComparer.OrdinalIgnoreCase);
            AppendSection(sb, "Duplicate type+name", dupes);
            AppendSection(sb, "High fan-in/out (≥" + FanThreshold + ")", highFan);
            return sb.ToString();
        }

        private static void AppendSection(StringBuilder sb, string title, List<string> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            sb.Append("- ").Append(title).Append(" (").Append(items.Count).Append("):\n");
            int n = 0;
            foreach (string item in items)
            {
                if (n >= MaxDetailLines)
                {
                    sb.Append("  … ").Append(items.Count - n).Append(" more\n");
                    break;
                }

                sb.Append("  • ").Append(item).Append('\n');
                n++;
            }
        }

        private static void Bump(Dictionary<string, int> map, string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (!map.ContainsKey(id!))
            {
                map[id!] = 0;
            }

            map[id!]++;
        }

        private static int Count(Dictionary<string, int> map, string? id)
        {
            if (string.IsNullOrEmpty(id) || !map.TryGetValue(id!, out int n))
            {
                return 0;
            }

            return n;
        }

        private static string Label(SnapshotElement e)
        {
            return e.Type + " \"" + e.Name + "\" (" + e.Id + ")";
        }

        private static string ShortRel(string? type)
        {
            string t = ArchiMateSchemaValidator.NormalizeRelationshipType(type);
            return t.EndsWith("Relationship", StringComparison.Ordinal)
                ? t.Substring(0, t.Length - "Relationship".Length)
                : t;
        }
    }
}
