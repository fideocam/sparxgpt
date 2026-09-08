using System;
using System.IO;
using System.Text;

namespace EaGpt
{
    /// <summary>
    /// Append-only NDJSON of applied mutations (archimate-mcp style audit trail).
    /// </summary>
    public static class AuditLog
    {
        public static string DefaultPath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(root))
            {
                root = Path.Combine(Path.GetTempPath(), "EaGpt");
            }

            return Path.Combine(root, "EaGpt", "audit.ndjson");
        }

        public static string Line(ArchiMateLlmResult result, string? prompt, bool applied)
        {
            var sb = new StringBuilder();
            sb.Append("{\"ts\":\"").Append(DateTime.UtcNow.ToString("o")).Append("\",");
            sb.Append("\"applied\":").Append(applied ? "true" : "false").Append(',');
            sb.Append("\"elements\":").Append(result.Elements.Count).Append(',');
            sb.Append("\"relationships\":").Append(result.Relationships.Count).Append(',');
            sb.Append("\"diagram\":").Append(result.Diagram != null ? "true" : "false").Append(',');
            sb.Append("\"removeElements\":").Append(result.RemoveElementIds.Count).Append(',');
            sb.Append("\"removeRelationships\":").Append(result.RemoveRelationshipIds.Count).Append(',');
            sb.Append("\"removeDiagrams\":").Append(result.RemoveDiagramNames.Count).Append(',');
            sb.Append("\"destructive\":").Append(MutationPolicy.IsDestructive(result) ? "true" : "false").Append(',');
            sb.Append("\"prompt\":\"").Append(JsonUtil.Escape(TrimPrompt(prompt))).Append("\"}");
            return sb.ToString();
        }

        public static void TryAppend(string? path, ArchiMateLlmResult result, string? prompt, bool applied)
        {
            try
            {
                string file = string.IsNullOrWhiteSpace(path) ? DefaultPath() : path!;
                string dir = Path.GetDirectoryName(file) ?? "";
                if (dir.Length > 0)
                {
                    Directory.CreateDirectory(dir);
                }

                File.AppendAllText(file, Line(result, prompt, applied) + Environment.NewLine);
            }
            catch
            {
                // audit must never block apply
            }
        }

        private static string TrimPrompt(string? prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return "";
            }

            string p = prompt!.Trim();
            return p.Length <= 180 ? p : p.Substring(0, 180);
        }
    }
}
