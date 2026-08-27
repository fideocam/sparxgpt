using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class RelationshipLegalityTests
    {
        [Fact]
        public void Access_ToBehavior_IsIllegal()
        {
            Assert.False(RelationshipLegality.IsAllowed("ApplicationComponent", "AccessRelationship", "BusinessProcess", out string reason));
            Assert.Contains("passive", reason);
            Assert.Contains("ServingRelationship", RelationshipLegality.SuggestedTypes("ApplicationComponent", "BusinessProcess"));
        }

        [Fact]
        public void Access_ToDataObject_IsLegal()
        {
            Assert.True(RelationshipLegality.IsAllowed("ApplicationFunction", "AccessRelationship", "DataObject"));
        }

        [Fact]
        public void Serving_ApplicationService_ToBusinessProcess_IsLegal()
        {
            Assert.True(RelationshipLegality.IsAllowed("ApplicationService", "ServingRelationship", "BusinessProcess"));
        }

        [Fact]
        public void Serving_WrongLayerDirection_IsIllegal()
        {
            Assert.False(RelationshipLegality.IsAllowed("BusinessService", "ServingRelationship", "ApplicationComponent"));
        }

        [Fact]
        public void Serving_FromPassive_IsIllegal()
        {
            Assert.False(RelationshipLegality.IsAllowed("BusinessObject", "ServingRelationship", "BusinessActor"));
        }

        [Fact]
        public void Triggering_FromActor_IsIllegal()
        {
            Assert.False(RelationshipLegality.IsAllowed("BusinessActor", "TriggeringRelationship", "BusinessProcess"));
        }

        [Fact]
        public void Assignment_RoleToProcess_IsLegal()
        {
            Assert.True(RelationshipLegality.IsAllowed("BusinessRole", "AssignmentRelationship", "BusinessProcess"));
        }

        [Fact]
        public void Realization_ComponentToService_IsLegal()
        {
            Assert.True(RelationshipLegality.IsAllowed("ApplicationComponent", "RealizationRelationship", "ApplicationService"));
        }

        [Fact]
        public void Realization_BusinessToTechnology_IsIllegal()
        {
            Assert.False(RelationshipLegality.IsAllowed("BusinessService", "RealizationRelationship", "Node"));
        }

        [Fact]
        public void Association_AlwaysLegal()
        {
            Assert.True(RelationshipLegality.IsAllowed("BusinessObject", "AssociationRelationship", "Node"));
        }

        [Fact]
        public void Composition_CrossLayer_IsIllegal()
        {
            Assert.False(RelationshipLegality.IsAllowed("BusinessActor", "CompositionRelationship", "ApplicationComponent"));
        }

        [Fact]
        public void Validate_UsesSnapshotTypes()
        {
            string app = "id-" + new string('a', 32);
            string proc = "id-" + new string('b', 32);
            var snap = new ModelSnapshot();
            snap.Elements.Add(new SnapshotElement { Id = app, Type = "ApplicationComponent", Name = "CRM" });
            snap.Elements.Add(new SnapshotElement { Id = proc, Type = "BusinessProcess", Name = "Hire" });

            var result = new ArchiMateLlmResult();
            result.Relationships.Add(new ArchiMateLlmResult.RelationshipSpec
            {
                Type = "AccessRelationship",
                Source = app,
                Target = proc
            });

            var errors = RelationshipLegality.Validate(result, snap);
            Assert.Contains(errors, e => e.Contains("Illegal ArchiMate relationship") && e.Contains("CRM"));
        }

        [Fact]
        public void Validate_SkipsUnresolvedEndpoints()
        {
            var result = new ArchiMateLlmResult();
            result.Relationships.Add(new ArchiMateLlmResult.RelationshipSpec
            {
                Type = "AccessRelationship",
                Source = "id-" + new string('c', 32),
                Target = "id-" + new string('d', 32)
            });
            Assert.Empty(RelationshipLegality.Validate(result, new ModelSnapshot()));
        }
    }
}
