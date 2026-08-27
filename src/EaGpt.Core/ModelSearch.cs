using System;
using System.Collections.Generic;
using System.Text;

namespace EaGpt
{
    /// <summary>
    /// Keyword search over the snapshot (OneRAI "find in repository", Sparx Japan find_elements_by_name).
    /// </summary>
    public static class ModelSearch
    {
        public static bool LooksLikeSearchQuery(string? prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return false;
            }

            string p = prompt!.ToLowerInvariant();
            return p.IndexOf("find ", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("search", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("list all", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("which ", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("where is", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("hae ", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("näytä", StringComparison.Ordinal) >= 0 ||
                   p.IndexOf("listaa", StringComparison.Ordinal) >= 0;
        }

        public static string Format(ModelSnapshot snapshot, string? prompt, int maxHits = 12)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(prompt))
            {
                return "";
            }

            string[] terms = KnowledgeRetriever.Tokenize(prompt);
            if (terms.Length == 0)
            {
                return "";
            }

            var scored = new List<(int Score, SnapshotElement El)>();
            foreach (var e in snapshot.Elements)
            {
                int score = Score(e, terms);
                if (score > 0)
                {
                    scored.Add((score, e));
                }
            }

            if (scored.Count == 0)
            {
                return LooksLikeSearchQuery(prompt) ? "SEARCH HITS: none.\n" : "";
            }

            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (!LooksLikeSearchQuery(prompt) && scored[0].Score < 6)
            {
                return "";
            }

            if (maxHits < 4)
            {
                maxHits = 4;
            }

            var sb = new StringBuilder();
            sb.Append("SEARCH HITS (").Append(Math.Min(maxHits, scored.Count)).Append(" of ").Append(scored.Count).Append("):\n");
            int n = 0;
            foreach (var hit in scored)
            {
                if (n >= maxHits)
                {
                    break;
                }

                sb.Append("- ").Append(hit.El.Type).Append(" \"").Append(hit.El.Name).Append("\" (")
                    .Append(hit.El.Id).Append(")\n");
                n++;
            }

            return sb.ToString();
        }

        private static int Score(SnapshotElement e, string[] terms)
        {
            string name = (e.Name ?? "").ToLowerInvariant();
            string type = (e.Type ?? "").ToLowerInvariant();
            int score = 0;
            foreach (string term in terms)
            {
                if (term.Length < 3)
                {
                    continue;
                }

                if (name == term)
                {
                    score += 12;
                }
                else if (name.IndexOf(term, StringComparison.Ordinal) >= 0)
                {
                    score += 6;
                }
                else if (type.IndexOf(term, StringComparison.Ordinal) >= 0)
                {
                    score += 2;
                }
            }

            return score;
        }
    }
}
