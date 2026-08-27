using System.Collections.Generic;

namespace EaGpt
{
    public sealed class ChatTurn
    {
        public string User { get; set; } = "";
        public string Assistant { get; set; } = "";
    }

    /// <summary>
    /// Prior user/assistant turns sent with the next Ollama request (OneRAI / EA Model Chat follow-ups).
    /// Stores the short prompt, not the XML digest, so stale model dumps do not crowd the context.
    /// </summary>
    public static class ChatHistory
    {
        public const int MaxTurns = 4;
        public const int MaxUserChars = 800;
        public const int MaxAssistantChars = 2500;

        public static void Remember(IList<ChatTurn> turns, string? userPrompt, string? assistant)
        {
            if (turns == null || string.IsNullOrWhiteSpace(userPrompt) || string.IsNullOrWhiteSpace(assistant))
            {
                return;
            }

            turns.Add(new ChatTurn
            {
                User = Trim(userPrompt!, MaxUserChars),
                Assistant = Trim(assistant!, MaxAssistantChars)
            });
            while (turns.Count > MaxTurns)
            {
                turns.RemoveAt(0);
            }
        }

        public static string Trim(string text, int maxChars)
        {
            string t = text.Trim();
            if (t.Length <= maxChars)
            {
                return t;
            }

            return t.Substring(0, maxChars) + "…";
        }
    }
}
