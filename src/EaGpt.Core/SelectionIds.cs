using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EaGpt
{
    public static class SelectionIds
    {
        private static readonly Regex IdEq = new Regex(@"id=(id-[0-9a-fA-F]{32})", RegexOptions.Compiled);
        private static readonly Regex OpenDiagram = new Regex(
            @"Primary diagram \(open in editor\) ""([^""]+)""",
            RegexOptions.Compiled);

        public static List<string> Parse(string? selectionContext)
        {
            var ids = new List<string>();
            if (string.IsNullOrEmpty(selectionContext))
            {
                return ids;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in IdEq.Matches(selectionContext))
            {
                string id = m.Groups[1].Value;
                if (seen.Add(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        public static string? OpenDiagramName(string? selectionContext)
        {
            if (string.IsNullOrEmpty(selectionContext))
            {
                return null;
            }

            Match m = OpenDiagram.Match(selectionContext);
            return m.Success ? m.Groups[1].Value : null;
        }

        public static bool LooksLikeImpactQuery(string? prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return false;
            }

            string p = prompt!.ToLowerInvariant();
            return p.IndexOf("impact", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("depend", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("who uses", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("what uses", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("affected", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("ripple", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("vaikutus", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("riippu", StringComparison.Ordinal) >= 0;
        }

        public static List<string> FindNamedElements(ModelSnapshot snapshot, string? prompt, int max = 6)
        {
            var ids = new List<string>();
            if (string.IsNullOrWhiteSpace(prompt) || snapshot.Elements.Count == 0)
            {
                return ids;
            }

            string p = prompt!.ToLowerInvariant();
            foreach (var e in snapshot.Elements)
            {
                if (ids.Count >= max || string.IsNullOrWhiteSpace(e.Name) || e.Name.Trim().Length < 3)
                {
                    continue;
                }

                if (p.IndexOf(e.Name.Trim().ToLowerInvariant(), StringComparison.Ordinal) >= 0)
                {
                    ids.Add(e.Id);
                }
            }

            return ids;
        }
    }
}
