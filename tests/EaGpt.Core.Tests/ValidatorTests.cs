using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class ValidatorTests
    {
        [Fact]
        public void Normalize_Aliases()
        {
            Assert.Equal("BusinessActor", ArchiMateSchemaValidator.NormalizeElementType("Actor"));
            Assert.Equal("Node", ArchiMateSchemaValidator.NormalizeElementType("Technology Node"));
            Assert.Equal("ApplicationComponent", ArchiMateSchemaValidator.NormalizeElementType("Component"));
            Assert.Equal("ServingRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("UsedBy"));
            Assert.Equal("ServingRelationship", ArchiMateSchemaValidator.NormalizeRelationshipType("Serving"));
        }

        [Fact]
        public void Validate_RejectsUnknownType()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec
            {
                Type = "Spaceship",
                Name = "X",
                Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            });
            var errors = ArchiMateSchemaValidator.Validate(result);
            Assert.Contains(errors, e => e.Contains("Spaceship"));
        }

        [Fact]
        public void Validate_AcceptsCanonicalTypes()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec
            {
                Type = "BusinessProcess",
                Name = "Hire",
                Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            });
            result.Relationships.Add(new ArchiMateLlmResult.RelationshipSpec
            {
                Type = "TriggeringRelationship",
                Source = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                Target = "id-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                Id = "id-cccccccccccccccccccccccccccccccc"
            });
            Assert.Empty(ArchiMateSchemaValidator.Validate(result));
        }

        [Fact]
        public void Validate_RejectsMissingIdAndRelationshipEndpoints()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "BusinessActor", Name = "X", Id = "  " });
            result.Relationships.Add(new ArchiMateLlmResult.RelationshipSpec { Type = "Serving", Source = "", Target = "" });
            var errors = ArchiMateSchemaValidator.Validate(result);
            Assert.Contains(errors, e => e.Contains("missing id"));
            Assert.Contains(errors, e => e.Contains("missing source"));
            Assert.Contains(errors, e => e.Contains("missing target"));
        }

        [Fact]
        public void Validate_SkipsViewPlaceholderElements()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "View", Name = "Overview", Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" });
            Assert.Empty(ArchiMateSchemaValidator.Validate(result));
        }
    }
}
