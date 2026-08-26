using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EaGpt
{
    /// <summary>
    /// Normalizes LLM type names to ArchiMate 3.2 EClass names and validates a parsed result.
    /// </summary>
    public static class ArchiMateSchemaValidator
    {
        private static readonly IReadOnlyDictionary<string, string> ElementAliases;
        private static readonly IReadOnlyDictionary<string, string> RelationshipAliases;
        private static readonly HashSet<string> ElementTypes;
        private static readonly HashSet<string> RelationshipTypes;

        static ArchiMateSchemaValidator()
        {
            var el = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Register(el,
                "Junction",
                "ApplicationCollaboration", "ApplicationComponent", "ApplicationEvent", "ApplicationFunction",
                "ApplicationInteraction", "ApplicationInterface", "ApplicationProcess", "ApplicationService",
                "Artifact", "Assessment",
                "BusinessActor", "BusinessCollaboration", "BusinessEvent", "BusinessFunction",
                "BusinessInteraction", "BusinessInterface", "BusinessObject", "BusinessProcess",
                "BusinessRole", "BusinessService",
                "Capability", "CommunicationNetwork", "Contract", "Constraint", "CourseOfAction",
                "DataObject", "Deliverable", "Device", "DistributionNetwork", "Driver",
                "Equipment", "Facility", "Gap", "Goal", "Grouping", "ImplementationEvent",
                "Location", "Material", "Meaning", "Node", "Outcome", "Path", "Plateau",
                "Principle", "Product", "Representation", "Requirement", "Resource",
                "Stakeholder", "SystemSoftware",
                "TechnologyCollaboration", "TechnologyEvent", "TechnologyFunction", "TechnologyInteraction",
                "TechnologyInterface", "TechnologyProcess", "TechnologyService",
                "Value", "ValueStream", "WorkPackage");
            Alias(el, "Actor", "BusinessActor");
            Alias(el, "Person", "BusinessActor");
            Alias(el, "Organisation", "BusinessActor");
            Alias(el, "Organization", "BusinessActor");
            Alias(el, "Role", "BusinessRole");
            Alias(el, "Process", "BusinessProcess");
            Alias(el, "Activity", "BusinessProcess");
            Alias(el, "Function", "BusinessFunction");
            Alias(el, "Service", "BusinessService");
            Alias(el, "Event", "BusinessEvent");
            Alias(el, "Object", "BusinessObject");
            Alias(el, "Collaboration", "BusinessCollaboration");
            Alias(el, "Interaction", "BusinessInteraction");
            Alias(el, "Component", "ApplicationComponent");
            Alias(el, "Application", "ApplicationComponent");
            Alias(el, "Module", "ApplicationComponent");
            Alias(el, "Interface", "ApplicationInterface");
            Alias(el, "API", "ApplicationInterface");
            Alias(el, "Data", "DataObject");
            Alias(el, "Entity", "DataObject");
            Alias(el, "Software", "SystemSoftware");
            Alias(el, "Server", "Node");
            Alias(el, "Host", "Node");
            Alias(el, "TechnologyNode", "Node");
            Alias(el, "InfrastructureNode", "Node");
            Alias(el, "Network", "CommunicationNetwork");
            Alias(el, "InfrastructureService", "TechnologyService");
            Alias(el, "InfrastructureFunction", "TechnologyFunction");
            Alias(el, "AndJunction", "Junction");
            Alias(el, "OrJunction", "Junction");
            ElementAliases = new ReadOnlyDictionary<string, string>(el);
            ElementTypes = new HashSet<string>(el.Values, StringComparer.Ordinal);

            var rel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Register(rel,
                "AccessRelationship", "AggregationRelationship", "AssignmentRelationship",
                "AssociationRelationship", "CompositionRelationship", "FlowRelationship",
                "InfluenceRelationship", "RealizationRelationship", "ServingRelationship",
                "SpecializationRelationship", "TriggeringRelationship");
            Alias(rel, "UsedByRelationship", "ServingRelationship");
            Alias(rel, "UsesRelationship", "ServingRelationship");
            Alias(rel, "UseRelationship", "ServingRelationship");
            Alias(rel, "ServesRelationship", "ServingRelationship");
            Alias(rel, "AccessesRelationship", "AccessRelationship");
            Alias(rel, "AssignedToRelationship", "AssignmentRelationship");
            Alias(rel, "RealizesRelationship", "RealizationRelationship");
            Alias(rel, "RealisationRelationship", "RealizationRelationship");
            Alias(rel, "TriggersRelationship", "TriggeringRelationship");
            Alias(rel, "FlowsToRelationship", "FlowRelationship");
            Alias(rel, "InfluencesRelationship", "InfluenceRelationship");
            Alias(rel, "ContainsRelationship", "CompositionRelationship");
            Alias(rel, "SpecialisationRelationship", "SpecializationRelationship");
            Alias(rel, "InteractionRelationship", "AssociationRelationship");
            Alias(rel, "DependencyRelationship", "AssociationRelationship");
            RelationshipAliases = new ReadOnlyDictionary<string, string>(rel);
            RelationshipTypes = new HashSet<string>(rel.Values, StringComparer.Ordinal);
        }

        public static IReadOnlyCollection<string> CanonicalElementTypes => ElementTypes;
        public static IReadOnlyCollection<string> CanonicalRelationshipTypes => RelationshipTypes;

        public static string NormalizeElementType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return type ?? "";
            }

            string t = type!.Trim().Replace(" ", "");
            if (ElementAliases.TryGetValue(t, out string? aliased) && aliased != null)
            {
                return aliased;
            }

            return t;
        }

        public static string NormalizeRelationshipType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return type ?? "";
            }

            string t = type!.Trim().Replace(" ", "");
            if (!t.EndsWith("Relationship", StringComparison.OrdinalIgnoreCase) &&
                !t.Equals("Junction", StringComparison.OrdinalIgnoreCase))
            {
                t += "Relationship";
            }

            if (RelationshipAliases.TryGetValue(t, out string? aliased) && aliased != null)
            {
                return aliased;
            }

            return t;
        }

        public static List<string> Validate(ArchiMateLlmResult result)
        {
            var errors = new List<string>();
            foreach (var e in result.Elements)
            {
                if (string.Equals(e.Type, "View", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(e.Type, "Diagram", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(e.Id))
                {
                    errors.Add("Element missing id: type=" + e.Type + ", name=" + e.Name);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(e.Type))
                {
                    errors.Add("Element missing type: id=" + e.Id);
                    continue;
                }

                string normalized = NormalizeElementType(e.Type);
                if (!ElementTypes.Contains(normalized))
                {
                    errors.Add("Invalid ArchiMate element type: " + e.Type + " (id=" + e.Id + ")");
                }
            }

            foreach (var r in result.Relationships)
            {
                if (string.IsNullOrWhiteSpace(r.Type))
                {
                    errors.Add("Relationship missing type: source=" + r.Source + " target=" + r.Target);
                    continue;
                }

                string relType = NormalizeRelationshipType(r.Type);
                if (!RelationshipTypes.Contains(relType))
                {
                    errors.Add("Invalid ArchiMate relationship type: " + r.Type);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(r.Source))
                {
                    errors.Add("Relationship missing source: type=" + r.Type);
                }

                if (string.IsNullOrWhiteSpace(r.Target))
                {
                    errors.Add("Relationship missing target: type=" + r.Type);
                }
            }

            return errors;
        }

        private static void Register(Dictionary<string, string> map, params string[] names)
        {
            foreach (string name in names)
            {
                map[name.ToLowerInvariant()] = name;
            }
        }

        private static void Alias(Dictionary<string, string> map, string from, string to)
        {
            map[from.Replace(" ", "").ToLowerInvariant()] = to;
        }
    }
}
