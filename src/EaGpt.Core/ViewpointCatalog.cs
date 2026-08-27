using System;
using System.Text;

namespace EaGpt
{
    /// <summary>
    /// Named viewpoint recipes injected when the user asks for a known view kind.
    /// Same role as Archi MCP "viewpoint recipe" resources, kept in-process for small Ollama models.
    /// </summary>
    public static class ViewpointCatalog
    {
        public sealed class Recipe
        {
            public Recipe(string name, string[] aliases, string body)
            {
                Name = name;
                Aliases = aliases;
                Body = body;
            }

            public string Name { get; }
            public string[] Aliases { get; }
            public string Body { get; }
        }

        public static readonly Recipe[] All =
        {
            new Recipe("business", new[] { "business", "liiketoiminta", "actor", "process view" },
                "Typical elements: BusinessActor, BusinessRole, BusinessProcess, BusinessFunction, BusinessService, BusinessObject.\n" +
                "Typical relationships: Assignment (actor/role → process), Triggering or Flow (process → process), Serving (service → process/role), Access (process → object).\n" +
                "Do not put Node or Device on a business-layer diagram unless the user asked for a mixed view."),
            new Recipe("application", new[] { "application", "sovellus", "app landscape", "application layer" },
                "Typical elements: ApplicationComponent, ApplicationInterface, ApplicationService, ApplicationProcess, DataObject.\n" +
                "Typical relationships: Serving (service/interface → user), Realization (component → service), Access (behavior → DataObject), Composition/Aggregation for parts.\n" +
                "Reuse CMDB application names from COMPANY KNOWLEDGE when present."),
            new Recipe("technology", new[] { "technology", "deployment", "infrastructure", "infrastruktuuri", "hosting" },
                "Typical elements: Node, Device, SystemSoftware, CommunicationNetwork, TechnologyService, Artifact, Path.\n" +
                "Typical relationships: Assignment (artifact/software → node), Serving (technology service → application), Realization where it applies.\n" +
                "Layout: one Node per runtime, SystemSoftware for OS/runtime, Artifact for the deployable. Skip BusinessActor unless asked."),
            new Recipe("motivation", new[] { "motivation", "strategy", "capability", "goal", "requirement", "principle" },
                "Typical elements: Stakeholder, Driver, Assessment, Goal, Outcome, Principle, Requirement, Constraint, Capability, Resource, CourseOfAction.\n" +
                "Typical relationships: Influence (driver/assessment → goal), Realization (requirement → goal/principle; resource → capability), Association if nothing tighter fits."),
            new Recipe("implementation", new[] { "implementation", "migration", "plateau", "work package", "roadmap" },
                "Typical elements: WorkPackage, Deliverable, ImplementationEvent, Plateau, Gap, plus the core elements a plateau represents.\n" +
                "Typical relationships: Realization (work package/plateau → architecture), Triggering (events), Association to gaps."),
            new Recipe("tiedonhallinta", new[]
                {
                    "tiedonhallintamalli", "tiedonhallinta", "tietovaranto", "tietoaineisto", "toimintaprosessi",
                    "tietojärjestelmä", "tekninen rajapinta", "katseluyhteys", "asiakirjajulkisuuskuvaus",
                    "information management", "data store"
                },
                "Finnish tiedonhallintamalli (Act 906/2019 5 § modelling aid, not legal advice). Cover all four minimum sets:\n" +
                "1) Toimintaprosessit → BusinessProcess + BusinessRole (vastaava viranomainen), purpose in notes, Triggering/Flow to other processes.\n" +
                "2) Tietovarannot → DataObject (Grouping if needed); link Access to processes and Serving/Access to systems; purpose, tietoryhmät, luovutuskohteet, retention in notes or Constraint; reuse yhteinen tietovaranto (once-only) instead of duplicating collection.\n" +
                "3) Tietoaineistot → DataObject/BusinessObject with archive transfer or destruction (Constraint/notes).\n" +
                "4) Tietojärjestelmät → ApplicationComponent + owner role + ApplicationInterface; prefer tekninen rajapinta (Serving/Flow) between authorities; name katseluyhteys explicitly if it is a view connection.\n" +
                "New systems: add a short muutosvaikutusten arviointi note (security, disclosure, asianhallinta, julkisuus, interoperability). Asiakirjajulkisuuskuvaus is a thinner public extract (28 §), not a substitute for the full model. Reuse CMDB names; do not invent existing ids.")
        };

        public static Recipe? Match(string? prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return null;
            }

            string p = prompt!.ToLowerInvariant();
            Recipe? best = null;
            int bestLen = 0;
            foreach (var recipe in All)
            {
                foreach (string alias in recipe.Aliases)
                {
                    if (alias.Length > bestLen && p.IndexOf(alias, StringComparison.Ordinal) >= 0)
                    {
                        best = recipe;
                        bestLen = alias.Length;
                    }
                }
            }

            return best;
        }

        public static string FormatForPrompt(string? prompt)
        {
            var sb = new StringBuilder();
            sb.Append("VIEWPOINT RECIPES available: business, application, technology, motivation, implementation, tiedonhallinta.\n");
            Recipe? matched = Match(prompt);
            if (matched != null)
            {
                sb.Append("VIEWPOINT RECIPE (").Append(matched.Name).Append("):\n").Append(matched.Body).Append('\n');
            }

            return sb.ToString();
        }
    }
}
