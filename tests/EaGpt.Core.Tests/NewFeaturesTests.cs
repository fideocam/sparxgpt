using System;
using System.Collections.Generic;
using System.IO;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class ArchimateAspectsTests
    {
        [Theory]
        [InlineData("BusinessActor", ArchimateLayer.Business, ArchimateAspect.ActiveStructure)]
        [InlineData("DataObject", ArchimateLayer.Application, ArchimateAspect.PassiveStructure)]
        [InlineData("Goal", ArchimateLayer.Motivation, ArchimateAspect.Motivation)]
        [InlineData("Node", ArchimateLayer.Technology, ArchimateAspect.ActiveStructure)]
        [InlineData("Junction", ArchimateLayer.Junction, ArchimateAspect.Junction)]
        public void Classify_KnownTypes(string type, ArchimateLayer layer, ArchimateAspect aspect)
        {
            var c = ArchimateAspects.Classify(type);
            Assert.Equal(layer, c.Layer);
            Assert.Equal(aspect, c.Aspect);
        }

        [Fact]
        public void Classify_Unknown_IsUnknown()
        {
            var c = ArchimateAspects.Classify("Spaceship");
            Assert.Equal(ArchimateLayer.Unknown, c.Layer);
        }

        [Fact]
        public void LayoutRows_RunMotivationToImplementation()
        {
            Assert.True(ArchimateAspects.LayoutRow(ArchimateLayer.Motivation) < ArchimateAspects.LayoutRow(ArchimateLayer.Business));
            Assert.True(ArchimateAspects.LayoutRow(ArchimateLayer.Business) < ArchimateAspects.LayoutRow(ArchimateLayer.Application));
            Assert.True(ArchimateAspects.LayoutRow(ArchimateLayer.Application) < ArchimateAspects.LayoutRow(ArchimateLayer.Technology));
            Assert.True(ArchimateAspects.IsCoreLayer(ArchimateLayer.Business));
            Assert.False(ArchimateAspects.IsCoreLayer(ArchimateLayer.Motivation));
        }
    }

    public class RelationshipLegalityMoreTests
    {
        [Fact]
        public void Flow_And_Triggering_BetweenBehavior()
        {
            Assert.True(RelationshipLegality.IsAllowed("BusinessProcess", "FlowRelationship", "BusinessProcess"));
            Assert.True(RelationshipLegality.IsAllowed("BusinessEvent", "TriggeringRelationship", "BusinessProcess"));
            Assert.False(RelationshipLegality.IsAllowed("BusinessActor", "FlowRelationship", "BusinessProcess"));
        }

        [Fact]
        public void Influence_RequiresMotivationOrStrategy()
        {
            Assert.True(RelationshipLegality.IsAllowed("Driver", "InfluenceRelationship", "Goal"));
            Assert.True(RelationshipLegality.IsAllowed("Capability", "InfluenceRelationship", "Outcome"));
            Assert.False(RelationshipLegality.IsAllowed("ApplicationComponent", "InfluenceRelationship", "ApplicationComponent"));
        }

        [Fact]
        public void Specialization_SameFamilyOnly()
        {
            Assert.True(RelationshipLegality.IsAllowed("BusinessActor", "SpecializationRelationship", "BusinessActor"));
            Assert.False(RelationshipLegality.IsAllowed("BusinessActor", "SpecializationRelationship", "BusinessProcess"));
        }

        [Fact]
        public void Junction_AndUnknownTypes_AreAllowed()
        {
            Assert.True(RelationshipLegality.IsAllowed("Junction", "AccessRelationship", "BusinessProcess"));
            Assert.True(RelationshipLegality.IsAllowed("Spaceship", "AccessRelationship", "BusinessProcess"));
        }

        [Fact]
        public void Serving_ToPassive_IsIllegal()
        {
            Assert.False(RelationshipLegality.IsAllowed("ApplicationService", "ServingRelationship", "DataObject"));
        }

        [Fact]
        public void Realization_SpecialCases()
        {
            Assert.True(RelationshipLegality.IsAllowed("DataObject", "RealizationRelationship", "BusinessObject"));
            Assert.True(RelationshipLegality.IsAllowed("Requirement", "RealizationRelationship", "Goal"));
            Assert.True(RelationshipLegality.IsAllowed("Artifact", "RealizationRelationship", "DataObject"));
            Assert.True(RelationshipLegality.IsAllowed("WorkPackage", "RealizationRelationship", "ApplicationComponent"));
        }

        [Fact]
        public void Assignment_ArtifactToNode_AndCompositionSameLayer()
        {
            Assert.True(RelationshipLegality.IsAllowed("Artifact", "AssignmentRelationship", "Node"));
            Assert.True(RelationshipLegality.IsAllowed("ApplicationComponent", "CompositionRelationship", "ApplicationComponent"));
            Assert.True(RelationshipLegality.IsAllowed("Grouping", "AggregationRelationship", "BusinessActor"));
        }

        [Fact]
        public void Validate_UsesNewMutationElements()
        {
            string app = "id-" + new string('e', 32);
            string proc = "id-" + new string('f', 32);
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "ApplicationComponent", Name = "ERP", Id = app });
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "BusinessProcess", Name = "Pay", Id = proc });
            result.Relationships.Add(new ArchiMateLlmResult.RelationshipSpec
            {
                Type = "AccessRelationship",
                Source = app,
                Target = proc
            });
            var errors = RelationshipLegality.Validate(result, snapshot: null);
            Assert.Contains(errors, e => e.Contains("ERP") && e.Contains("Suggestions"));
        }

        [Fact]
        public void SuggestedTypes_AlwaysIncludesAssociation()
        {
            var types = RelationshipLegality.SuggestedTypes("BusinessObject", "Node");
            Assert.Contains("AssociationRelationship", types);
            Assert.True(types.Count <= 4);
        }
    }

    public class ImpactMermaidSelectionTests
    {
        private static ModelSnapshot Chain()
        {
            string a = "id-" + new string('a', 32);
            string b = "id-" + new string('b', 32);
            var snap = new ModelSnapshot();
            snap.Elements.Add(new SnapshotElement { Id = a, Type = "BusinessProcess", Name = "Start" });
            snap.Elements.Add(new SnapshotElement { Id = b, Type = "BusinessProcess", Name = "Next" });
            snap.Relationships.Add(new SnapshotRelationship
            {
                Id = "id-" + new string('r', 32),
                Type = "TriggeringRelationship",
                Source = a,
                Target = b
            });
            snap.Diagrams.Add(new SnapshotDiagram { Name = "Flow" });
            snap.Diagrams[0].Nodes.Add(new SnapshotNode { ElementId = a });
            snap.Diagrams[0].Nodes.Add(new SnapshotNode { ElementId = b });
            return snap;
        }

        [Fact]
        public void Impact_EmptyOrUnknownSeeds_IsEmpty()
        {
            Assert.Equal("", ImpactAnalyzer.Format(Chain(), new string[0]));
            Assert.Equal("", ImpactAnalyzer.Format(Chain(), new[] { "id-" + new string('z', 32) }));
        }

        [Fact]
        public void Impact_ListsIncomingAndDedupesSeeds()
        {
            var snap = Chain();
            string target = snap.Elements[1].Id;
            string text = ImpactAnalyzer.Format(snap, new[] { target, target });
            Assert.Contains("incoming", text);
            Assert.Contains("Start", text);
            Assert.Equal(1, CountOccurrences(text, "BusinessProcess \"Next\""));
        }

        [Fact]
        public void Mermaid_EmptyWithoutSeeds_IsEmpty()
        {
            Assert.Equal("", MermaidExporter.Neighborhood(Chain(), new string[0], selectionContext: null));
        }

        [Fact]
        public void Mermaid_FromDiagram_AndEscapesQuotes()
        {
            var snap = Chain();
            snap.Elements[0].Name = "Say \"Hi\" [x]";
            string mermaid = MermaidExporter.FromDiagram(snap, snap.Diagrams[0]);
            Assert.Contains("```mermaid", mermaid);
            Assert.Contains("Say 'Hi' (x)", mermaid);
            Assert.DoesNotContain("[x]", mermaid);
        }

        [Fact]
        public void SelectionIds_DedupesAndIgnoresShortNames()
        {
            string id = "id-" + new string('a', 32);
            string ctx = "id=" + id + " and again id=" + id;
            Assert.Equal(new[] { id }, SelectionIds.Parse(ctx));
            Assert.Empty(SelectionIds.Parse(""));
            Assert.Null(SelectionIds.OpenDiagramName("no diagram here"));
            Assert.False(SelectionIds.LooksLikeImpactQuery("describe this view"));
            Assert.True(SelectionIds.LooksLikeImpactQuery("mikä on vaikutus"));

            var snap = new ModelSnapshot();
            snap.Elements.Add(new SnapshotElement { Id = id, Name = "AB", Type = "BusinessActor" });
            snap.Elements.Add(new SnapshotElement { Id = "id-" + new string('b', 32), Name = "Customer", Type = "BusinessActor" });
            Assert.Empty(SelectionIds.FindNamedElements(snap, "look at AB please"));
            Assert.Single(SelectionIds.FindNamedElements(snap, "Customer onboarding", max: 1));
        }

        private static int CountOccurrences(string hay, string needle)
        {
            int n = 0, from = 0;
            while (true)
            {
                int i = hay.IndexOf(needle, from, StringComparison.Ordinal);
                if (i < 0)
                {
                    return n;
                }

                n++;
                from = i + needle.Length;
            }
        }
    }

    public class ViewpointAndLayoutMoreTests
    {
        [Theory]
        [InlineData("liiketoiminta näkymä", "business")]
        [InlineData("sovellus landscape", "application")]
        [InlineData("tietovaranto inventory", "tiedonhallinta")]
        [InlineData("tiedonhallintamalli 5 pykala", "tiedonhallinta")]
        [InlineData("tekninen rajapinta viranomaisten valilla", "tiedonhallinta")]
        [InlineData("migration plateau", "implementation")]
        [InlineData("capability map", "motivation")]
        public void Viewpoint_Aliases(string prompt, string name)
        {
            var recipe = ViewpointCatalog.Match(prompt);
            Assert.NotNull(recipe);
            Assert.Equal(name, recipe!.Name);
        }

        [Fact]
        public void Viewpoint_EmptyPrompt_ListsRecipesOnly()
        {
            Assert.Null(ViewpointCatalog.Match(""));
            string text = ViewpointCatalog.FormatForPrompt("just chatting");
            Assert.Contains("VIEWPOINT RECIPES available", text);
            Assert.DoesNotContain("VIEWPOINT RECIPE (", text);
        }

        [Fact]
        public void Tiedonhallinta_Recipe_CoversFiveSectionObjects()
        {
            string text = ViewpointCatalog.FormatForPrompt("tiedonhallintamalli");
            Assert.Contains("VIEWPOINT RECIPE (tiedonhallinta)", text);
            Assert.Contains("Toimintaprosessit", text);
            Assert.Contains("Tietovarannot", text);
            Assert.Contains("Tietoaineistot", text);
            Assert.Contains("Tietojärjestelmät", text);
            Assert.Contains("tekninen rajapinta", text);
            Assert.Contains("not legal advice", text);
        }

        [Fact]
        public void Layout_NoDiagram_IsNoOp()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "BusinessActor", Name = "A", Id = "id-" + new string('1', 32) });
            DiagramLayout.Prepare(result, null);
            Assert.Null(result.Diagram);
        }

        [Fact]
        public void Layout_SkipsViewPlaceholders_AndFixesTinyBox()
        {
            var result = new ArchiMateLlmResult();
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "V" };
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "View", Name = "Ignore", Id = "id-" + new string('9', 32) });
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "BusinessActor", Name = "A", Id = "id-" + new string('1', 32) });
            result.Diagram.Nodes.Add(new ArchiMateLlmResult.DiagramNodeSpec
            {
                ElementId = "id-" + new string('1', 32),
                X = 0,
                Y = 0,
                Width = 1,
                Height = 1
            });
            DiagramLayout.Prepare(result, null);
            Assert.Single(result.Diagram.Nodes);
            Assert.Equal(DiagramLayout.OriginX, result.Diagram.Nodes[0].X);
            Assert.Equal(120, result.Diagram.Nodes[0].Width);
            Assert.Equal(55, result.Diagram.Nodes[0].Height);
        }

        [Fact]
        public void Layout_UsesSnapshotTypesForExistingIds()
        {
            string biz = "id-" + new string('a', 32);
            string app = "id-" + new string('b', 32);
            var snap = new ModelSnapshot();
            snap.Elements.Add(new SnapshotElement { Id = biz, Type = "BusinessActor", Name = "A" });
            snap.Elements.Add(new SnapshotElement { Id = app, Type = "ApplicationComponent", Name = "B" });
            var result = new ArchiMateLlmResult();
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "Mix" };
            result.Diagram.Nodes.Add(new ArchiMateLlmResult.DiagramNodeSpec { ElementId = biz, X = 0, Y = 0, Width = 120, Height = 55 });
            result.Diagram.Nodes.Add(new ArchiMateLlmResult.DiagramNodeSpec { ElementId = app, X = 0, Y = 0, Width = 120, Height = 55 });
            DiagramLayout.Prepare(result, snap);
            Assert.True(result.Diagram.Nodes[0].Y < result.Diagram.Nodes[1].Y);
        }
    }

    public class QualitySearchHistoryAuditTests
    {
        [Fact]
        public void Quality_EmptyModel_AndFinnishTrigger()
        {
            string text = ModelQuality.Format(new ModelSnapshot(), detailed: false);
            Assert.Contains("0 with no relationships", text);
            Assert.True(ModelQuality.LooksLikeAuditQuery("tarkista laatu"));
            Assert.False(ModelQuality.LooksLikeAuditQuery("create an actor"));
        }

        [Fact]
        public void Quality_HighFanOut_AndEmptyView()
        {
            string hub = "id-" + new string('h', 32);
            var snap = new ModelSnapshot();
            snap.Elements.Add(new SnapshotElement { Id = hub, Type = "ApplicationComponent", Name = "Hub" });
            snap.Diagrams.Add(new SnapshotDiagram { Name = "Empty" });
            for (int i = 0; i < ModelQuality.FanThreshold; i++)
            {
                string leaf = "id-" + i.ToString("x").PadLeft(32, '0');
                snap.Elements.Add(new SnapshotElement { Id = leaf, Type = "DataObject", Name = "D" + i });
                snap.Relationships.Add(new SnapshotRelationship
                {
                    Id = "id-r" + i.ToString("x").PadLeft(30, '0'),
                    Type = "AccessRelationship",
                    Source = hub,
                    Target = leaf
                });
            }

            string detail = ModelQuality.Format(snap, detailed: true);
            Assert.Contains("High fan-in/out", detail);
            Assert.Contains("Hub", detail);
            string summary = ModelQuality.Format(snap, detailed: false);
            Assert.Contains("1 empty view", summary);
        }

        [Fact]
        public void Search_None_WeakScore_AndFinnish()
        {
            var snap = new ModelSnapshot();
            snap.Elements.Add(new SnapshotElement { Id = "id-" + new string('a', 32), Type = "BusinessActor", Name = "Customer" });
            Assert.Equal("", ModelSearch.Format(snap, ""));
            Assert.Contains("SEARCH HITS: none", ModelSearch.Format(snap, "find XYZWidget"));
            Assert.Equal("", ModelSearch.Format(snap, "please look around"));
            Assert.True(ModelSearch.LooksLikeSearchQuery("näytä Customer"));
            Assert.False(ModelSearch.LooksLikeSearchQuery("findCRM"));
        }

        [Fact]
        public void History_SkipsBlank_AndTruncatesAssistant()
        {
            var turns = new List<ChatTurn>();
            ChatHistory.Remember(turns, "   ", "x");
            ChatHistory.Remember(turns, "q", "   ");
            Assert.Empty(turns);
            ChatHistory.Remember(turns, "q", new string('a', ChatHistory.MaxAssistantChars + 5));
            Assert.Single(turns);
            Assert.EndsWith("…", turns[0].Assistant);
            Assert.Equal(ChatHistory.MaxAssistantChars + 1, turns[0].Assistant.Length);
        }

        [Fact]
        public void ChatRequest_OmitsEmptyHistoryUser_KeepsUserWithoutAssistant()
        {
            var history = new List<ChatTurn>
            {
                new ChatTurn { User = "", Assistant = "nope" },
                new ChatTurn { User = "only user", Assistant = "" }
            };
            string json = OllamaClient.BuildChatRequestJson("m", "sys", "now", stream: true, history);
            Assert.Contains("\"stream\":true", json);
            Assert.DoesNotContain("nope", json);
            Assert.Contains("only user", json);
            Assert.Contains("now", json);
        }

        [Fact]
        public void AuditLog_EscapesTruncatesAndSwallowsIoErrors()
        {
            var result = new ArchiMateLlmResult();
            result.RemoveElementIds.Add("x");
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "D" };
            string line = AuditLog.Line(result, "say \"hi\" " + new string('z', 200), applied: false);
            Assert.Contains("\"applied\":false", line);
            Assert.Contains("\"destructive\":true", line);
            Assert.Contains("\"diagram\":true", line);
            Assert.Contains("say \\\"hi\\\"", line);
            Assert.True(line.IndexOf("prompt", StringComparison.Ordinal) >= 0);
            Assert.DoesNotContain(new string('z', 200), line);
            Assert.Contains("EaGpt", AuditLog.DefaultPath());

            string dir = Path.Combine(Path.GetTempPath(), "eagpt-audit-dir-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            AuditLog.TryAppend(dir, result, "x", applied: true);
        }

        [Fact]
        public void Preview_EmptyDiagramAndRemovals()
        {
            Assert.Equal("Preview: no model changes.", MutationPolicy.PreviewSummary(new ArchiMateLlmResult()));
            var result = new ArchiMateLlmResult();
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "Hire" };
            result.Diagram.Nodes.Add(new ArchiMateLlmResult.DiagramNodeSpec { ElementId = "a" });
            result.RemoveElementFromDiagramIds.Add("a");
            result.RemoveRelationshipIds.Add("r");
            string preview = MutationPolicy.PreviewSummary(result);
            Assert.Contains("create diagram \"Hire\" (1 node", preview);
            Assert.Contains("figure", preview);
            Assert.Contains("delete 1 relationship", preview);
            Assert.Contains("Preview:", MutationPolicy.DestructiveSummary(result));
        }
    }

    public class OpenAiCompatAndPromptTests
    {
        [Fact]
        public void ExtractChatDelta_OpenAiNonStreamAndEmpty()
        {
            const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Hello from LM Studio\"}}]}";
            Assert.Equal("Hello from LM Studio", OllamaJson.ExtractChatDelta(body));
            Assert.Equal("", OllamaJson.ExtractChatDelta(""));
            Assert.Equal("", OllamaJson.ExtractChatDelta("{\"choices\":[]}"));
            Assert.Empty(OllamaJson.ParseOpenAiModelIds(""));
            Assert.Empty(OllamaJson.ParseOpenAiModelIds("{}"));
        }

        [Fact]
        public void SystemPrompt_MentionsNewContextBlocks()
        {
            string prompt = ArchiMateSystemPrompt.GetSystemPrompt();
            Assert.Contains("MODEL QUALITY", prompt);
            Assert.Contains("SEARCH HITS", prompt);
            Assert.Contains("Prior chat turns", prompt);
            Assert.Contains("VIEWPOINT RECIPE", prompt);
        }

        [Fact]
        public void UserMessage_PutsAnalysisBeforeKnowledge()
        {
            string msg = UserMessageBuilder.BuildUserMessage(
                "sel",
                "<archimate/>",
                "audit the model",
                knowledge: "KNOW",
                analysisContext: "MODEL QUALITY: 0 issues.\n");
            Assert.True(msg.IndexOf("MODEL QUALITY", StringComparison.Ordinal) > msg.IndexOf("--- END OF MODEL ---", StringComparison.Ordinal));
            Assert.True(msg.IndexOf("KNOW", StringComparison.Ordinal) > msg.IndexOf("MODEL QUALITY", StringComparison.Ordinal));
        }

        [Fact]
        public void AnalysisContext_ImpactWithoutSelection_UsesName()
        {
            string actor = "id-" + new string('a', 32);
            var snap = new ModelSnapshot();
            snap.Elements.Add(new SnapshotElement { Id = actor, Type = "BusinessActor", Name = "Customer" });
            snap.Elements.Add(new SnapshotElement { Id = "id-" + new string('b', 32), Type = "BusinessProcess", Name = "Hire" });
            snap.Relationships.Add(new SnapshotRelationship
            {
                Id = "id-" + new string('r', 32),
                Type = "AssignmentRelationship",
                Source = actor,
                Target = snap.Elements[1].Id
            });
            string ctx = ModelAnalysisContext.Build(snap, selectionContext: null, prompt: "what depends on Customer");
            Assert.Contains("IMPACT ANALYSIS", ctx);
            Assert.Contains("Customer", ctx);
        }
    }
}
