using System.Collections.Generic;

namespace EaGpt
{
    /// <summary>
    /// Parsed LLM payload: ArchiMate 3 elements, relationships, optional new diagram, and removals.
    /// Same JSON shape as ArchiGPT.
    /// </summary>
    public sealed class ArchiMateLlmResult
    {
        public List<ElementSpec> Elements { get; } = new List<ElementSpec>();
        public List<RelationshipSpec> Relationships { get; } = new List<RelationshipSpec>();
        public List<string> RemoveElementIds { get; } = new List<string>();
        public List<string> RemoveRelationshipIds { get; } = new List<string>();
        public List<string> RemoveDiagramNames { get; } = new List<string>();
        public List<string> RemoveElementFromDiagramIds { get; } = new List<string>();
        public List<string> RemoveRelationshipFromDiagramIds { get; } = new List<string>();
        public DiagramSpec? Diagram { get; set; }
        public string? Error { get; set; }

        public bool HasMutations =>
            Elements.Count > 0 ||
            Relationships.Count > 0 ||
            RemoveElementIds.Count > 0 ||
            RemoveRelationshipIds.Count > 0 ||
            RemoveDiagramNames.Count > 0 ||
            RemoveElementFromDiagramIds.Count > 0 ||
            RemoveRelationshipFromDiagramIds.Count > 0 ||
            Diagram != null;

        public sealed class DiagramSpec
        {
            public string? Name { get; set; }
            public string? Viewpoint { get; set; }
            public List<DiagramNodeSpec> Nodes { get; } = new List<DiagramNodeSpec>();
            public List<DiagramConnectionSpec> Connections { get; } = new List<DiagramConnectionSpec>();
        }

        public sealed class DiagramNodeSpec
        {
            public string? ElementId { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; } = 120;
            public int Height { get; set; } = 55;
        }

        public sealed class DiagramConnectionSpec
        {
            public string? SourceElementId { get; set; }
            public string? TargetElementId { get; set; }
            public string? RelationshipId { get; set; }
        }

        public sealed class ElementSpec
        {
            public string? Type { get; set; }
            public string? Name { get; set; }
            public string? Id { get; set; }
        }

        public sealed class RelationshipSpec
        {
            public string? Type { get; set; }
            public string? Source { get; set; }
            public string? Target { get; set; }
            public string? Name { get; set; }
            public string? Id { get; set; }
        }
    }
}
