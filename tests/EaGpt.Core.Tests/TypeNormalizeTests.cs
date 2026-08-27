using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    /// <summary>
    /// Type aliases used when the LLM invents or uses ArchiMate 2 names.
    /// Mirrors ArchiGPT ArchiMateTypeNormalizeTest (commit 919aa88).
    /// </summary>
    public class TypeNormalizeTests
    {
        [Fact]
        public void InteractionRelationship_MapsToAssociation()
        {
            Assert.Equal("AssociationRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("InteractionRelationship"));
            Assert.Equal("AssociationRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("interaction"));
            Assert.Equal("AssociationRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("Interaction Relationship"));
        }

        [Fact]
        public void UsedByRelationship_MapsToServing()
        {
            Assert.Equal("ServingRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("UsedByRelationship"));
            Assert.Equal("ServingRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("uses"));
        }

        [Fact]
        public void OfficialRelationshipNames_Unchanged()
        {
            Assert.Equal("AssignmentRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("AssignmentRelationship"));
            Assert.Equal("FlowRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("FlowRelationship"));
            Assert.Equal("AssignmentRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("Assignment"));
        }

        [Fact]
        public void InteractionElement_MapsToBusinessInteraction()
        {
            Assert.Equal("BusinessInteraction", ArchiMateSchemaValidator.NormalizeElementType("Interaction"));
            Assert.Equal("BusinessActor", ArchiMateSchemaValidator.NormalizeElementType("BusinessActor"));
        }

        [Fact]
        public void UnqualifiedElementNames_MapToBusinessOrApplicationLayer()
        {
            Assert.Equal("BusinessActor", ArchiMateSchemaValidator.NormalizeElementType("Actor"));
            Assert.Equal("BusinessProcess", ArchiMateSchemaValidator.NormalizeElementType("Process"));
            Assert.Equal("BusinessService", ArchiMateSchemaValidator.NormalizeElementType("Service"));
            Assert.Equal("ApplicationComponent", ArchiMateSchemaValidator.NormalizeElementType("Component"));
            Assert.Equal("ApplicationInterface", ArchiMateSchemaValidator.NormalizeElementType("Interface"));
            Assert.Equal("Node", ArchiMateSchemaValidator.NormalizeElementType("Server"));
            Assert.Equal("DataObject", ArchiMateSchemaValidator.NormalizeElementType("Database"));
            Assert.Equal("CommunicationNetwork", ArchiMateSchemaValidator.NormalizeElementType("Network"));
        }

        [Fact]
        public void Archimate2Infrastructure_MapsToTechnology()
        {
            Assert.Equal("TechnologyService", ArchiMateSchemaValidator.NormalizeElementType("InfrastructureService"));
            Assert.Equal("TechnologyInterface", ArchiMateSchemaValidator.NormalizeElementType("InfrastructureInterface"));
            Assert.Equal("Path", ArchiMateSchemaValidator.NormalizeElementType("CommunicationPath"));
            Assert.Equal("Node", ArchiMateSchemaValidator.NormalizeElementType("TechnologyNode"));
        }

        [Fact]
        public void BritishSpelling_Relationships()
        {
            Assert.Equal("RealizationRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("RealisationRelationship"));
            Assert.Equal("SpecializationRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("SpecialisationRelationship"));
            Assert.Equal("AssociationRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("RelatedTo"));
        }

        [Fact]
        public void OfficialNames_CaseInsensitive()
        {
            Assert.Equal("BusinessActor", ArchiMateSchemaValidator.NormalizeElementType("businessactor"));
            Assert.Equal("ServingRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("servingrelationship"));
        }

        [Fact]
        public void Validate_AcceptsLlmMixUps()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec
            {
                Type = "Database",
                Name = "Orders",
                Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            });
            result.Relationships.Add(new ArchiMateLlmResult.RelationshipSpec
            {
                Type = "InteractionRelationship",
                Source = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                Target = "id-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
            });
            Assert.Empty(ArchiMateSchemaValidator.Validate(result));
            Assert.Equal("ArchiMate3::ArchiMate_DataObject", ArchiMateEaTypeMap.ElementFqType("Database"));
            Assert.Equal("ArchiMate3::ArchiMate_Association", ArchiMateEaTypeMap.RelationshipFqType("InteractionRelationship"));
        }
    }
}
