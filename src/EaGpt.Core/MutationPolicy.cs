using System.Collections.Generic;

namespace EaGpt
{
    /// <summary>
    /// Limits and confirmation rules for applying LLM-produced model mutations.
    /// </summary>
    public static class MutationPolicy
    {
        public const int MaxReplyChars = 200_000;
        public const int MaxElements = 80;
        public const int MaxRelationships = 120;
        public const int MaxRemovals = 50;
        public const int MaxNameChars = 256;
        public const int MaxIdChars = 80;
        public const int MaxCoord = 4000;

        /// <summary>
        /// Deletes from the model or of whole diagrams. These require an explicit user confirmation.
        /// </summary>
        public static bool IsDestructive(ArchiMateLlmResult result)
        {
            return result.RemoveElementIds.Count > 0 ||
                   result.RemoveRelationshipIds.Count > 0 ||
                   result.RemoveDiagramNames.Count > 0;
        }

        public static List<string> CheckLimits(ArchiMateLlmResult result)
        {
            var errors = new List<string>();
            if (result.Elements.Count > MaxElements)
            {
                errors.Add("Too many elements in one reply (" + result.Elements.Count + " > " + MaxElements + ").");
            }

            if (result.Relationships.Count > MaxRelationships)
            {
                errors.Add("Too many relationships in one reply (" + result.Relationships.Count + " > " + MaxRelationships + ").");
            }

            int removals = result.RemoveElementIds.Count + result.RemoveRelationshipIds.Count +
                           result.RemoveDiagramNames.Count + result.RemoveElementFromDiagramIds.Count +
                           result.RemoveRelationshipFromDiagramIds.Count;
            if (removals > MaxRemovals)
            {
                errors.Add("Too many removals in one reply (" + removals + " > " + MaxRemovals + ").");
            }

            foreach (var e in result.Elements)
            {
                CheckName(errors, e.Name, "Element");
                CheckId(errors, e.Id, "Element");
            }

            foreach (var r in result.Relationships)
            {
                CheckName(errors, r.Name, "Relationship");
                CheckId(errors, r.Id, "Relationship");
                CheckId(errors, r.Source, "Relationship source");
                CheckId(errors, r.Target, "Relationship target");
            }

            CheckStringList(errors, result.RemoveElementIds, "Removal element id");
            CheckStringList(errors, result.RemoveRelationshipIds, "Removal relationship id");
            CheckStringList(errors, result.RemoveElementFromDiagramIds, "Diagram-removal element id");
            CheckStringList(errors, result.RemoveRelationshipFromDiagramIds, "Diagram-removal relationship id");
            foreach (string name in result.RemoveDiagramNames)
            {
                CheckName(errors, name, "Removed diagram");
            }

            if (result.Diagram != null)
            {
                CheckName(errors, result.Diagram.Name, "Diagram");
                if (result.Diagram.Nodes.Count > MaxElements)
                {
                    errors.Add("Too many diagram nodes in one reply (" + result.Diagram.Nodes.Count + " > " + MaxElements + ").");
                }
            }

            return errors;
        }

        private static void CheckName(List<string> errors, string? name, string what)
        {
            if (name != null && name.Length > MaxNameChars)
            {
                errors.Add(what + " name is too long (" + name.Length + " > " + MaxNameChars + ").");
            }
        }

        private static void CheckId(List<string> errors, string? id, string what)
        {
            if (id != null && id.Length > MaxIdChars)
            {
                errors.Add(what + " id is too long (" + id.Length + " > " + MaxIdChars + ").");
            }
        }

        private static void CheckStringList(List<string> errors, List<string> ids, string what)
        {
            foreach (string id in ids)
            {
                CheckId(errors, id, what);
            }
        }

        public static string DestructiveSummary(ArchiMateLlmResult result)
        {
            return "EaGPT wants to delete " +
                   result.RemoveElementIds.Count + " element(s), " +
                   result.RemoveRelationshipIds.Count + " relationship(s), and " +
                   result.RemoveDiagramNames.Count + " diagram(s) from the model. Continue?";
        }
    }
}
