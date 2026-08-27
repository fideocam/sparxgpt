using System;
using System.Collections.Generic;

namespace EaGpt
{
    public enum ArchimateLayer
    {
        Unknown = 0,
        Motivation = 1,
        Strategy = 2,
        Business = 3,
        Application = 4,
        Technology = 5,
        Implementation = 6,
        Composite = 7,
        Junction = 8
    }

    public enum ArchimateAspect
    {
        Unknown = 0,
        ActiveStructure = 1,
        Behavior = 2,
        PassiveStructure = 3,
        Motivation = 4,
        Composite = 5,
        Junction = 6
    }

    /// <summary>
    /// ArchiMate 3.2 layer/aspect classification used by legality checks and layout.
    /// </summary>
    public static class ArchimateAspects
    {
        private static readonly Dictionary<string, (ArchimateLayer Layer, ArchimateAspect Aspect)> Map =
            new Dictionary<string, (ArchimateLayer, ArchimateAspect)>(StringComparer.OrdinalIgnoreCase)
            {
                ["Stakeholder"] = (ArchimateLayer.Motivation, ArchimateAspect.ActiveStructure),
                ["Driver"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),
                ["Assessment"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),
                ["Goal"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),
                ["Outcome"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),
                ["Principle"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),
                ["Requirement"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),
                ["Constraint"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),
                ["Meaning"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),
                ["Value"] = (ArchimateLayer.Motivation, ArchimateAspect.Motivation),

                ["Resource"] = (ArchimateLayer.Strategy, ArchimateAspect.ActiveStructure),
                ["Capability"] = (ArchimateLayer.Strategy, ArchimateAspect.Behavior),
                ["ValueStream"] = (ArchimateLayer.Strategy, ArchimateAspect.Behavior),
                ["CourseOfAction"] = (ArchimateLayer.Strategy, ArchimateAspect.Behavior),

                ["BusinessActor"] = (ArchimateLayer.Business, ArchimateAspect.ActiveStructure),
                ["BusinessRole"] = (ArchimateLayer.Business, ArchimateAspect.ActiveStructure),
                ["BusinessCollaboration"] = (ArchimateLayer.Business, ArchimateAspect.ActiveStructure),
                ["BusinessInterface"] = (ArchimateLayer.Business, ArchimateAspect.ActiveStructure),
                ["BusinessProcess"] = (ArchimateLayer.Business, ArchimateAspect.Behavior),
                ["BusinessFunction"] = (ArchimateLayer.Business, ArchimateAspect.Behavior),
                ["BusinessInteraction"] = (ArchimateLayer.Business, ArchimateAspect.Behavior),
                ["BusinessEvent"] = (ArchimateLayer.Business, ArchimateAspect.Behavior),
                ["BusinessService"] = (ArchimateLayer.Business, ArchimateAspect.Behavior),
                ["BusinessObject"] = (ArchimateLayer.Business, ArchimateAspect.PassiveStructure),
                ["Contract"] = (ArchimateLayer.Business, ArchimateAspect.PassiveStructure),
                ["Representation"] = (ArchimateLayer.Business, ArchimateAspect.PassiveStructure),
                ["Product"] = (ArchimateLayer.Business, ArchimateAspect.Composite),

                ["ApplicationComponent"] = (ArchimateLayer.Application, ArchimateAspect.ActiveStructure),
                ["ApplicationCollaboration"] = (ArchimateLayer.Application, ArchimateAspect.ActiveStructure),
                ["ApplicationInterface"] = (ArchimateLayer.Application, ArchimateAspect.ActiveStructure),
                ["ApplicationFunction"] = (ArchimateLayer.Application, ArchimateAspect.Behavior),
                ["ApplicationInteraction"] = (ArchimateLayer.Application, ArchimateAspect.Behavior),
                ["ApplicationProcess"] = (ArchimateLayer.Application, ArchimateAspect.Behavior),
                ["ApplicationEvent"] = (ArchimateLayer.Application, ArchimateAspect.Behavior),
                ["ApplicationService"] = (ArchimateLayer.Application, ArchimateAspect.Behavior),
                ["DataObject"] = (ArchimateLayer.Application, ArchimateAspect.PassiveStructure),

                ["Node"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["Device"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["SystemSoftware"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["TechnologyCollaboration"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["TechnologyInterface"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["Path"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["CommunicationNetwork"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["Equipment"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["Facility"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["DistributionNetwork"] = (ArchimateLayer.Technology, ArchimateAspect.ActiveStructure),
                ["TechnologyFunction"] = (ArchimateLayer.Technology, ArchimateAspect.Behavior),
                ["TechnologyProcess"] = (ArchimateLayer.Technology, ArchimateAspect.Behavior),
                ["TechnologyInteraction"] = (ArchimateLayer.Technology, ArchimateAspect.Behavior),
                ["TechnologyEvent"] = (ArchimateLayer.Technology, ArchimateAspect.Behavior),
                ["TechnologyService"] = (ArchimateLayer.Technology, ArchimateAspect.Behavior),
                ["Artifact"] = (ArchimateLayer.Technology, ArchimateAspect.PassiveStructure),
                ["Material"] = (ArchimateLayer.Technology, ArchimateAspect.PassiveStructure),

                ["WorkPackage"] = (ArchimateLayer.Implementation, ArchimateAspect.Behavior),
                ["Deliverable"] = (ArchimateLayer.Implementation, ArchimateAspect.PassiveStructure),
                ["ImplementationEvent"] = (ArchimateLayer.Implementation, ArchimateAspect.Behavior),
                ["Plateau"] = (ArchimateLayer.Implementation, ArchimateAspect.Composite),
                ["Gap"] = (ArchimateLayer.Implementation, ArchimateAspect.PassiveStructure),

                ["Grouping"] = (ArchimateLayer.Composite, ArchimateAspect.Composite),
                ["Location"] = (ArchimateLayer.Composite, ArchimateAspect.Composite),
                ["Junction"] = (ArchimateLayer.Junction, ArchimateAspect.Junction)
            };

        public static (ArchimateLayer Layer, ArchimateAspect Aspect) Classify(string? type)
        {
            string canon = ArchiMateSchemaValidator.NormalizeElementType(type);
            if (Map.TryGetValue(canon, out var pair))
            {
                return pair;
            }

            return (ArchimateLayer.Unknown, ArchimateAspect.Unknown);
        }

        public static bool IsCoreLayer(ArchimateLayer layer)
        {
            return layer == ArchimateLayer.Business ||
                   layer == ArchimateLayer.Application ||
                   layer == ArchimateLayer.Technology;
        }

        public static int LayoutRow(ArchimateLayer layer)
        {
            switch (layer)
            {
                case ArchimateLayer.Motivation: return 0;
                case ArchimateLayer.Strategy: return 1;
                case ArchimateLayer.Business: return 2;
                case ArchimateLayer.Application: return 3;
                case ArchimateLayer.Technology: return 4;
                case ArchimateLayer.Implementation: return 5;
                default: return 6;
            }
        }

        public static string LayerName(ArchimateLayer layer)
        {
            return layer.ToString();
        }
    }
}
