using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EaGpt
{
    /// <summary>
    /// Pulls a small set of local knowledge files into the LLM prompt (lightweight RAG).
    /// Put Markdown/text under the knowledge folder; retrieval is keyword overlap for now.
    /// </summary>
    public static class KnowledgeRetriever
    {
        public const int DefaultMaxChars = 8000;
        public const int MaxFiles = 200;
        public const int MaxFileChars = 32_000;
        public const int MaxChunks = 8;
        public static readonly string[] AllowedExtensions = { ".md", ".txt", ".csv" };

        public static string DefaultFolder()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(root))
            {
                root = Path.Combine(Path.GetTempPath(), "EaGpt");
            }

            return Path.Combine(root, "EaGpt", "knowledge");
        }

        public static string Retrieve(string? folder, string? query, int maxChars = DefaultMaxChars)
        {
            if (maxChars < 500)
            {
                maxChars = 500;
            }

            if (maxChars > 40_000)
            {
                maxChars = 40_000;
            }

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return "";
            }

            var files = new List<string>();
            try
            {
                foreach (string ext in AllowedExtensions)
                {
                    files.AddRange(Directory.EnumerateFiles(folder, "*" + ext, SearchOption.AllDirectories));
                }
            }
            catch
            {
                return "";
            }

            if (files.Count == 0)
            {
                return "";
            }

            if (files.Count > MaxFiles)
            {
                files = files.GetRange(0, MaxFiles);
            }

            string[] terms = Tokenize(query);
            var scored = new List<(int Score, string Rel, string Body)>();
            string rootFull = Path.GetFullPath(folder);
            foreach (string file in files)
            {
                string full;
                try
                {
                    full = Path.GetFullPath(file);
                    if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string body = File.ReadAllText(full);
                    if (body.Length > MaxFileChars)
                    {
                        body = body.Substring(0, MaxFileChars);
                    }

                    string rel = full.Substring(rootFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace('\\', '/');
                    int score = Score(rel, body, terms);
                    if (score > 0 || terms.Length == 0)
                    {
                        scored.Add((score, rel, body.Trim()));
                    }
                }
                catch
                {
                    // skip unreadable files
                }
            }

            if (scored.Count == 0)
            {
                return "";
            }

            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            var sb = new StringBuilder();
            sb.Append("--- COMPANY KNOWLEDGE (retrieved for this request) ---\n");
            int used = sb.Length;
            int n = 0;
            foreach (var item in scored)
            {
                if (n >= MaxChunks)
                {
                    break;
                }

                string header = "### " + item.Rel + "\n";
                int budget = maxChars - used - 80;
                if (budget < 120)
                {
                    break;
                }

                string body = item.Body;
                if (header.Length + body.Length + 2 > budget)
                {
                    body = body.Substring(0, Math.Max(0, budget - header.Length - 20)) + "\n[truncated]";
                }

                sb.Append(header).Append(body).Append("\n\n");
                used = sb.Length;
                n++;
            }

            sb.Append("--- END OF KNOWLEDGE ---\n");
            return sb.ToString();
        }

        internal static string[] Tokenize(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<string>();
            }

            var list = new List<string>();
            var current = new StringBuilder();
            foreach (char c in query!.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    current.Append(c);
                }
                else if (current.Length > 0)
                {
                    AddTerm(list, current.ToString());
                    current.Clear();
                }
            }

            if (current.Length > 0)
            {
                AddTerm(list, current.ToString());
            }

            return list.Distinct().ToArray();
        }

        private static void AddTerm(List<string> list, string term)
        {
            if (term.Length < 3)
            {
                return;
            }

            if (term == "the" || term == "and" || term == "for" || term == "with" ||
                term == "this" || term == "that" || term == "add" ||
                term == "jaa" || term == "että" || term == "kun")
            {
                return;
            }

            list.Add(term);
        }

        private static int Score(string relativePath, string body, string[] terms)
        {
            if (terms.Length == 0)
            {
                return 1;
            }

            string hay = (relativePath + "\n" + body).ToLowerInvariant();
            int score = 0;
            foreach (string term in terms)
            {
                if (relativePath.ToLowerInvariant().Contains(term))
                {
                    score += 8;
                }

                int from = 0;
                int hits = 0;
                while (hits < 12)
                {
                    int i = hay.IndexOf(term, from, StringComparison.Ordinal);
                    if (i < 0)
                    {
                        break;
                    }

                    hits++;
                    from = i + term.Length;
                }

                score += hits;
            }

            return score;
        }
    }
}
