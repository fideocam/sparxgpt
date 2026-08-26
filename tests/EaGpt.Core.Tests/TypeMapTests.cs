using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class TypeMapTests
    {
        [Fact]
        public void ElementFqType_UsesArchiMate3Profile()
        {
            Assert.Equal("ArchiMate3::ArchiMate_BusinessActor", ArchiMateEaTypeMap.ElementFqType("BusinessActor"));
            Assert.Equal("ArchiMate3::ArchiMate_BusinessActor", ArchiMateEaTypeMap.ElementFqType("Actor"));
            Assert.Equal("ArchiMate3::ArchiMate_Node", ArchiMateEaTypeMap.ElementFqType("TechnologyNode"));
        }

        [Fact]
        public void RelationshipFqType_MapsServing()
        {
            Assert.Equal("ArchiMate3::ArchiMate_Serving", ArchiMateEaTypeMap.RelationshipFqType("ServingRelationship"));
            Assert.Equal("ArchiMate3::ArchiMate_Serving", ArchiMateEaTypeMap.RelationshipFqType("UsedBy"));
        }

        [Fact]
        public void DiagramFqType_FromViewpoint()
        {
            Assert.Equal("ArchiMate3::Application", ArchiMateEaTypeMap.DiagramFqType("application"));
            Assert.Equal("ArchiMate3::Business", ArchiMateEaTypeMap.DiagramFqType("default"));
        }

        [Fact]
        public void FromEaStereotype_Reverses()
        {
            Assert.Equal("BusinessActor", ArchiMateEaTypeMap.FromEaStereotype("ArchiMate3::ArchiMate_BusinessActor", false));
            Assert.Equal("ServingRelationship", ArchiMateEaTypeMap.FromEaStereotype("ArchiMate_Serving", true));
        }
    }
}
