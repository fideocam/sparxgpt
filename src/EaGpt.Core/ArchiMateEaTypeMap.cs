using System;
using System.Collections.Generic;

namespace EaGpt
{
    /// <summary>
    /// Maps ArchiMate 3.2 type names to Sparx EA ArchiMate3 MDG fully-qualified stereotypes.
    /// AddNew type strings follow the Automation Interface (e.g. ArchiMate3::ArchiMate_BusinessActor).
    /// </summary>
    public static class ArchiMateEaTypeMap
    {
        public const string Profile = "ArchiMate3";

        private static readonly Dictionary<string, string> ElementStereotype = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BusinessActor"] = "ArchiMate_BusinessActor",
            ["BusinessRole"] = "ArchiMate_BusinessRole",
            ["BusinessCollaboration"] = "ArchiMate_BusinessCollaboration",
            ["BusinessInterface"] = "ArchiMate_BusinessInterface",
            ["BusinessProcess"] = "ArchiMate_BusinessProcess",
            ["BusinessFunction"] = "ArchiMate_BusinessFunction",
            ["BusinessInteraction"] = "ArchiMate_BusinessInteraction",
            ["BusinessEvent"] = "ArchiMate_BusinessEvent",
            ["BusinessService"] = "ArchiMate_BusinessService",
            ["BusinessObject"] = "ArchiMate_BusinessObject",
            ["Contract"] = "ArchiMate_Contract",
            ["Representation"] = "ArchiMate_Representation",
            ["Product"] = "ArchiMate_Product",
            ["ApplicationComponent"] = "ArchiMate_ApplicationComponent",
            ["ApplicationCollaboration"] = "ArchiMate_ApplicationCollaboration",
            ["ApplicationInterface"] = "ArchiMate_ApplicationInterface",
            ["ApplicationFunction"] = "ArchiMate_ApplicationFunction",
            ["ApplicationInteraction"] = "ArchiMate_ApplicationInteraction",
            ["ApplicationProcess"] = "ArchiMate_ApplicationProcess",
            ["ApplicationEvent"] = "ArchiMate_ApplicationEvent",
            ["ApplicationService"] = "ArchiMate_ApplicationService",
            ["DataObject"] = "ArchiMate_DataObject",
            ["Node"] = "ArchiMate_Node",
            ["Device"] = "ArchiMate_Device",
            ["SystemSoftware"] = "ArchiMate_SystemSoftware",
            ["TechnologyCollaboration"] = "ArchiMate_TechnologyCollaboration",
            ["TechnologyInterface"] = "ArchiMate_TechnologyInterface",
            ["Path"] = "ArchiMate_Path",
            ["CommunicationNetwork"] = "ArchiMate_CommunicationNetwork",
            ["TechnologyFunction"] = "ArchiMate_TechnologyFunction",
            ["TechnologyProcess"] = "ArchiMate_TechnologyProcess",
            ["TechnologyInteraction"] = "ArchiMate_TechnologyInteraction",
            ["TechnologyEvent"] = "ArchiMate_TechnologyEvent",
            ["TechnologyService"] = "ArchiMate_TechnologyService",
            ["Artifact"] = "ArchiMate_Artifact",
            ["Equipment"] = "ArchiMate_Equipment",
            ["Facility"] = "ArchiMate_Facility",
            ["DistributionNetwork"] = "ArchiMate_DistributionNetwork",
            ["Material"] = "ArchiMate_Material",
            ["Stakeholder"] = "ArchiMate_Stakeholder",
            ["Driver"] = "ArchiMate_Driver",
            ["Assessment"] = "ArchiMate_Assessment",
            ["Goal"] = "ArchiMate_Goal",
            ["Outcome"] = "ArchiMate_Outcome",
            ["Principle"] = "ArchiMate_Principle",
            ["Requirement"] = "ArchiMate_Requirement",
            ["Constraint"] = "ArchiMate_Constraint",
            ["Meaning"] = "ArchiMate_Meaning",
            ["Value"] = "ArchiMate_Value",
            ["Resource"] = "ArchiMate_Resource",
            ["Capability"] = "ArchiMate_Capability",
            ["CourseOfAction"] = "ArchiMate_CourseOfAction",
            ["ValueStream"] = "ArchiMate_ValueStream",
            ["WorkPackage"] = "ArchiMate_WorkPackage",
            ["Deliverable"] = "ArchiMate_Deliverable",
            ["ImplementationEvent"] = "ArchiMate_ImplementationEvent",
            ["Plateau"] = "ArchiMate_Plateau",
            ["Gap"] = "ArchiMate_Gap",
            ["Grouping"] = "ArchiMate_Grouping",
            ["Location"] = "ArchiMate_Location",
            ["Junction"] = "ArchiMate_Junction"
        };

        private static readonly Dictionary<string, string> RelationshipStereotype = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CompositionRelationship"] = "ArchiMate_Composition",
            ["AggregationRelationship"] = "ArchiMate_Aggregation",
            ["AssignmentRelationship"] = "ArchiMate_Assignment",
            ["RealizationRelationship"] = "ArchiMate_Realization",
            ["ServingRelationship"] = "ArchiMate_Serving",
            ["AccessRelationship"] = "ArchiMate_Access",
            ["InfluenceRelationship"] = "ArchiMate_Influence",
            ["AssociationRelationship"] = "ArchiMate_Association",
            ["SpecializationRelationship"] = "ArchiMate_Specialization",
            ["FlowRelationship"] = "ArchiMate_Flow",
            ["TriggeringRelationship"] = "ArchiMate_Triggering"
        };

        public static string ElementFqType(string archiMateType)
        {
            string canon = ArchiMateSchemaValidator.NormalizeElementType(archiMateType);
            if (!ElementStereotype.TryGetValue(canon, out string? stereo) || string.IsNullOrEmpty(stereo))
            {
                return "";
            }

            return Profile + "::" + stereo;
        }

        public static string RelationshipFqType(string archiMateType)
        {
            string canon = ArchiMateSchemaValidator.NormalizeRelationshipType(archiMateType);
            if (!RelationshipStereotype.TryGetValue(canon, out string? stereo) || string.IsNullOrEmpty(stereo))
            {
                return "";
            }

            return Profile + "::" + stereo;
        }

        /// <summary>
        /// Map a viewpoint or layer hint to an EA ArchiMate diagram type.
        /// </summary>
        public static string DiagramFqType(string? viewpoint)
        {
            string v = (viewpoint ?? "").Trim().ToLowerInvariant();
            if (v.Contains("application"))
            {
                return Profile + "::Application";
            }

            if (v.Contains("technology") || v.Contains("infrastructure"))
            {
                return Profile + "::Technology";
            }

            if (v.Contains("motivation") || v.Contains("strategy") || v.Contains("capability"))
            {
                return Profile + "::Motivation";
            }

            if (v.Contains("implementation") || v.Contains("migration"))
            {
                return Profile + "::Implementation";
            }

            return Profile + "::Business";
        }

        /// <summary>
        /// Best-effort reverse map from an EA stereotype (with or without profile) to an ArchiMate type name.
        /// </summary>
        public static string FromEaStereotype(string? stereotype, bool relationship)
        {
            if (string.IsNullOrWhiteSpace(stereotype))
            {
                return relationship ? "AssociationRelationship" : "BusinessObject";
            }

            string s = stereotype!.Trim();
            int sep = s.LastIndexOf("::", StringComparison.Ordinal);
            if (sep >= 0)
            {
                s = s.Substring(sep + 2);
            }

            if (s.StartsWith("ArchiMate_", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring("ArchiMate_".Length);
            }

            var map = relationship ? RelationshipStereotype : ElementStereotype;
            foreach (var kv in map)
            {
                if (kv.Value.Equals("ArchiMate_" + s, StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Equals(s, StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Equals(s + "Relationship", StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Key;
                }
            }

            return relationship ? s + (s.EndsWith("Relationship", StringComparison.Ordinal) ? "" : "Relationship") : s;
        }
    }
}
