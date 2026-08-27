using System;
using System.Collections.Generic;

namespace EaGpt
{
    /// <summary>
    /// ArchiMate 3.2-inspired relationship checks (aspect + layer heuristics, not the full official matrix).
    /// Invalid LLM links are rejected before EA apply, with suggested legal types — the same idea as
    /// Archi MCP servers (pyArchimate / archimate-mcp).
    /// </summary>
    public static class RelationshipLegality
    {
        private static readonly string[] AllRelTypes =
        {
            "CompositionRelationship", "AggregationRelationship", "AssignmentRelationship",
            "RealizationRelationship", "ServingRelationship", "AccessRelationship",
            "InfluenceRelationship", "AssociationRelationship", "SpecializationRelationship",
            "FlowRelationship", "TriggeringRelationship"
        };

        public static bool IsAllowed(string? sourceType, string? relationshipType, string? targetType)
        {
            return IsAllowed(sourceType, relationshipType, targetType, out _);
        }

        public static bool IsAllowed(string? sourceType, string? relationshipType, string? targetType, out string reason)
        {
            string src = ArchiMateSchemaValidator.NormalizeElementType(sourceType);
            string tgt = ArchiMateSchemaValidator.NormalizeElementType(targetType);
            string rel = ArchiMateSchemaValidator.NormalizeRelationshipType(relationshipType);
            var s = ArchimateAspects.Classify(src);
            var t = ArchimateAspects.Classify(tgt);

            if (s.Layer == ArchimateLayer.Unknown || t.Layer == ArchimateLayer.Unknown)
            {
                reason = "";
                return true;
            }

            if (s.Layer == ArchimateLayer.Junction || t.Layer == ArchimateLayer.Junction)
            {
                reason = "";
                return true;
            }

            switch (rel)
            {
                case "AssociationRelationship":
                    reason = "";
                    return true;

                case "AccessRelationship":
                    if (t.Aspect != ArchimateAspect.PassiveStructure)
                    {
                        reason = "Access must target a passive structure (BusinessObject, DataObject, Artifact, Contract, Representation, Material, Deliverable).";
                        return false;
                    }

                    if (s.Aspect != ArchimateAspect.ActiveStructure && s.Aspect != ArchimateAspect.Behavior)
                    {
                        reason = "Access source should be active structure or behavior.";
                        return false;
                    }

                    reason = "";
                    return true;

                case "ServingRelationship":
                    if (s.Aspect == ArchimateAspect.PassiveStructure)
                    {
                        reason = "A passive structure cannot serve; use Access or Association.";
                        return false;
                    }

                    if (t.Aspect == ArchimateAspect.PassiveStructure)
                    {
                        reason = "Serving targets an active structure or behavior, not a passive object. Did you mean Access?";
                        return false;
                    }

                    if (ArchimateAspects.IsCoreLayer(s.Layer) && ArchimateAspects.IsCoreLayer(t.Layer) &&
                        (int)s.Layer < (int)t.Layer)
                    {
                        reason = "Serving usually runs from a more concrete layer to a more abstract one (Technology → Application → Business).";
                        return false;
                    }

                    reason = "";
                    return true;

                case "RealizationRelationship":
                    if (!RealizationOk(src, tgt, s, t, out reason))
                    {
                        return false;
                    }

                    reason = "";
                    return true;

                case "AssignmentRelationship":
                    if (AssignmentOk(src, tgt, s, t))
                    {
                        reason = "";
                        return true;
                    }

                    reason = "Assignment is typically active structure → behavior (or actor/role, or artifact/software → node).";
                    return false;

                case "TriggeringRelationship":
                    if (s.Aspect == ArchimateAspect.Behavior && t.Aspect == ArchimateAspect.Behavior)
                    {
                        reason = "";
                        return true;
                    }

                    reason = "Triggering is between behavior elements (process, function, event, service, interaction). Use Assignment from an actor/role.";
                    return false;

                case "FlowRelationship":
                    if ((s.Aspect == ArchimateAspect.Behavior && t.Aspect == ArchimateAspect.Behavior) ||
                        (s.Aspect == ArchimateAspect.PassiveStructure && t.Aspect == ArchimateAspect.PassiveStructure))
                    {
                        reason = "";
                        return true;
                    }

                    reason = "Flow is typically between behavior elements, or between passive objects.";
                    return false;

                case "InfluenceRelationship":
                    if (s.Layer == ArchimateLayer.Motivation || t.Layer == ArchimateLayer.Motivation ||
                        s.Layer == ArchimateLayer.Strategy || t.Layer == ArchimateLayer.Strategy)
                    {
                        reason = "";
                        return true;
                    }

                    reason = "Influence involves Motivation or Strategy elements (Goal, Driver, Requirement, Capability, …).";
                    return false;

                case "SpecializationRelationship":
                    if (string.Equals(src, tgt, StringComparison.OrdinalIgnoreCase) ||
                        (s.Layer == t.Layer && s.Aspect == t.Aspect))
                    {
                        reason = "";
                        return true;
                    }

                    reason = "Specialization should stay in the same element family (same type, or same layer and aspect).";
                    return false;

                case "CompositionRelationship":
                case "AggregationRelationship":
                    if (s.Aspect == ArchimateAspect.Composite || s.Layer == ArchimateLayer.Composite ||
                        src.Equals("Node", StringComparison.OrdinalIgnoreCase) ||
                        src.Equals("Facility", StringComparison.OrdinalIgnoreCase) ||
                        src.Equals("Product", StringComparison.OrdinalIgnoreCase) ||
                        s.Layer == t.Layer)
                    {
                        reason = "";
                        return true;
                    }

                    reason = "Composition/aggregation stays in the same layer (or from Grouping, Location, Product, Node).";
                    return false;

                default:
                    reason = "";
                    return true;
            }
        }

        public static List<string> SuggestedTypes(string? sourceType, string? targetType, int max = 4)
        {
            var list = new List<string>();
            foreach (string rel in AllRelTypes)
            {
                if (IsAllowed(sourceType, rel, targetType) && list.Count < max)
                {
                    list.Add(rel);
                }
            }

            if (list.Count == 0)
            {
                list.Add("AssociationRelationship");
            }

            return list;
        }

        /// <summary>
        /// Validate relationships in an LLM mutation against new elements and the current snapshot.
        /// Unresolvable endpoints are skipped (truncated digest / unknown ids).
        /// </summary>
        public static List<string> Validate(ArchiMateLlmResult result, ModelSnapshot? snapshot)
        {
            var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (snapshot != null)
            {
                foreach (var e in snapshot.Elements)
                {
                    if (string.IsNullOrWhiteSpace(e.Id))
                    {
                        continue;
                    }

                    types[e.Id] = ArchiMateSchemaValidator.NormalizeElementType(e.Type);
                    names[e.Id] = e.Name ?? "";
                }
            }

            foreach (var e in result.Elements)
            {
                if (string.IsNullOrWhiteSpace(e.Id))
                {
                    continue;
                }

                types[e.Id.Trim()] = ArchiMateSchemaValidator.NormalizeElementType(e.Type);
                names[e.Id.Trim()] = e.Name ?? "";
            }

            var errors = new List<string>();
            foreach (var r in result.Relationships)
            {
                if (string.IsNullOrWhiteSpace(r.Source) || string.IsNullOrWhiteSpace(r.Target))
                {
                    continue;
                }

                if (!types.TryGetValue(r.Source.Trim(), out string? srcType) ||
                    !types.TryGetValue(r.Target.Trim(), out string? tgtType) ||
                    string.IsNullOrEmpty(srcType) || string.IsNullOrEmpty(tgtType))
                {
                    continue;
                }

                if (IsAllowed(srcType, r.Type, tgtType, out string reason))
                {
                    continue;
                }

                string srcName = names.TryGetValue(r.Source.Trim(), out string? sn) ? sn : "";
                string tgtName = names.TryGetValue(r.Target.Trim(), out string? tn) ? tn : "";
                string rel = ArchiMateSchemaValidator.NormalizeRelationshipType(r.Type);
                var suggestions = SuggestedTypes(srcType, tgtType);
                errors.Add(
                    "Illegal ArchiMate relationship: " + rel +
                    " from " + srcType + (srcName.Length > 0 ? " \"" + srcName + "\"" : "") +
                    " to " + tgtType + (tgtName.Length > 0 ? " \"" + tgtName + "\"" : "") +
                    ". " + reason +
                    " Suggestions: " + string.Join(", ", suggestions) + ".");
            }

            return errors;
        }

        private static bool AssignmentOk(string src, string tgt, (ArchimateLayer Layer, ArchimateAspect Aspect) s, (ArchimateLayer Layer, ArchimateAspect Aspect) t)
        {
            if ((src == "BusinessActor" || src == "BusinessRole") &&
                (tgt == "BusinessActor" || tgt == "BusinessRole"))
            {
                return true;
            }

            if (s.Aspect == ArchimateAspect.ActiveStructure && t.Aspect == ArchimateAspect.Behavior)
            {
                return true;
            }

            bool deploymentTarget = tgt == "Node" || tgt == "Device" || tgt == "Equipment" || tgt == "Facility";
            bool deployable = src == "Artifact" || src == "SystemSoftware" || src == "Device" || src == "Material";
            return deploymentTarget && deployable;
        }

        private static bool RealizationOk(
            string src,
            string tgt,
            (ArchimateLayer Layer, ArchimateAspect Aspect) s,
            (ArchimateLayer Layer, ArchimateAspect Aspect) t,
            out string reason)
        {
            reason = "Realization runs from a more concrete concept to a more abstract one (e.g. Application → Business, Artifact → DataObject, Requirement → Goal).";

            if (s.Layer == ArchimateLayer.Implementation)
            {
                return true;
            }

            if (s.Layer == ArchimateLayer.Composite || t.Layer == ArchimateLayer.Composite)
            {
                return true;
            }

            if (src == "DataObject" && (tgt == "BusinessObject" || tgt == "Contract" || tgt == "Representation"))
            {
                return true;
            }

            if (src == "Artifact" && (tgt == "DataObject" || tgt == "BusinessObject" || t.Layer == ArchimateLayer.Application))
            {
                return true;
            }

            if (src == "Representation" && (tgt == "BusinessObject" || tgt == "Contract"))
            {
                return true;
            }

            if (s.Layer == ArchimateLayer.Motivation && t.Layer == ArchimateLayer.Motivation)
            {
                bool srcReq = src == "Requirement" || src == "Constraint";
                bool tgtAbs = tgt == "Goal" || tgt == "Principle" || tgt == "Outcome" || tgt == "Value" || tgt == "Driver";
                if (srcReq && tgtAbs)
                {
                    return true;
                }

                if (src == "Goal" && (tgt == "Outcome" || tgt == "Value"))
                {
                    return true;
                }

                if (src == "Outcome" && tgt == "Goal")
                {
                    return false;
                }

                return srcReq || src == "Goal";
            }

            if (tgt == "Capability" || tgt == "CourseOfAction" || tgt == "ValueStream" || tgt == "Resource")
            {
                return s.Layer != ArchimateLayer.Motivation || src == "Requirement" || src == "Constraint";
            }

            if (ArchimateAspects.IsCoreLayer(s.Layer) && ArchimateAspects.IsCoreLayer(t.Layer))
            {
                return (int)s.Layer >= (int)t.Layer;
            }

            if (ArchimateAspects.IsCoreLayer(s.Layer) && t.Layer == ArchimateLayer.Strategy)
            {
                return true;
            }

            return false;
        }
    }
}
