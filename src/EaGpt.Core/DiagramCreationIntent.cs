using System;

namespace EaGpt
{
    /// <summary>
    /// Detects when the user explicitly asked for a brand-new diagram (vs adding to the current one).
    /// Same rule as ArchiGPT: if a diagram is open and the prompt is not for a new canvas, drop a
    /// spurious LLM <c>diagram</c> block so it does not create a second view.
    /// </summary>
    public static class DiagramCreationIntent
    {
        public static bool UserAskedForBrandNewView(string? prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return false;
            }

            string p = " " + prompt!.Trim().ToLowerInvariant().Replace('\n', ' ').Replace('\r', ' ') + " ";
            return ContainsPhrase(p, "new diagram")
                   || ContainsPhrase(p, "new view")
                   || ContainsPhrase(p, "another diagram")
                   || ContainsPhrase(p, "another view")
                   || ContainsPhrase(p, "second diagram")
                   || ContainsPhrase(p, "second view")
                   || ContainsPhrase(p, "create a new diagram")
                   || ContainsPhrase(p, "create a new view")
                   || ContainsPhrase(p, "add a new diagram")
                   || ContainsPhrase(p, "add a new view")
                   || ContainsPhrase(p, "add a diagram")
                   || ContainsPhrase(p, "add a view")
                   || ContainsPhrase(p, "create a diagram")
                   || ContainsPhrase(p, "create a view")
                   || ContainsPhrase(p, "create diagram")
                   || ContainsPhrase(p, "create view")
                   || ContainsPhrase(p, "design a diagram")
                   || ContainsPhrase(p, "design a view")
                   || ContainsPhrase(p, "diagram from scratch")
                   || ContainsPhrase(p, "make a new diagram")
                   || ContainsPhrase(p, "make a new view");
        }

        /// <summary>
        /// When a diagram is already open and the user did not ask for a new canvas, ignore the
        /// LLM's <c>diagram</c> object so figures land on the current view instead.
        /// </summary>
        public static bool TryDropUnwantedDiagram(ArchiMateLlmResult result, string? prompt, bool hasOpenDiagram)
        {
            if (result.Diagram == null || string.IsNullOrWhiteSpace(result.Diagram.Name))
            {
                return false;
            }

            if (!hasOpenDiagram || UserAskedForBrandNewView(prompt))
            {
                return false;
            }

            result.Diagram = null;
            return true;
        }

        private static bool ContainsPhrase(string spacedLower, string phrase)
        {
            return spacedLower.IndexOf(" " + phrase + " ", StringComparison.Ordinal) >= 0
                   || spacedLower.IndexOf(" " + phrase + ".", StringComparison.Ordinal) >= 0
                   || spacedLower.IndexOf(" " + phrase + ",", StringComparison.Ordinal) >= 0
                   || spacedLower.IndexOf(" " + phrase + "?", StringComparison.Ordinal) >= 0
                   || spacedLower.IndexOf(" " + phrase + "!", StringComparison.Ordinal) >= 0;
        }
    }
}
