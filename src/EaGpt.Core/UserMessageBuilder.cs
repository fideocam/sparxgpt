using System.Text;

namespace EaGpt
{
    public static class UserMessageBuilder
    {
        public static string BuildUserMessage(string? selectionContext, string? modelXml, string? prompt, string? knowledge = null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(modelXml))
            {
                sb.Append("ArchiMate model (compact XML):\n\n").Append(modelXml).Append("\n\n");
            }

            sb.Append("--- END OF MODEL ---\n\n");
            if (!string.IsNullOrWhiteSpace(knowledge))
            {
                sb.Append(knowledge!.Trim()).Append("\n\n");
            }

            sb.Append("User request: ").Append(prompt ?? "").Append("\n\n");
            if (!string.IsNullOrEmpty(selectionContext))
            {
                sb.Append(selectionContext);
            }

            return sb.ToString();
        }
    }
}
