using System.Linq;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class MutationPolicyTests
    {
        [Fact]
        public void IsDestructive_OnlyModelDeletes()
        {
            var add = new ArchiMateLlmResult();
            add.Elements.Add(new ArchiMateLlmResult.ElementSpec { Type = "BusinessActor", Name = "A", Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" });
            Assert.False(MutationPolicy.IsDestructive(add));

            var fromDiagram = new ArchiMateLlmResult();
            fromDiagram.RemoveElementFromDiagramIds.Add("id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            Assert.False(MutationPolicy.IsDestructive(fromDiagram));

            var del = new ArchiMateLlmResult();
            del.RemoveElementIds.Add("id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            Assert.True(MutationPolicy.IsDestructive(del));

            var diagrams = new ArchiMateLlmResult();
            diagrams.RemoveDiagramNames.Add("Idea");
            Assert.True(MutationPolicy.IsDestructive(diagrams));
        }

        [Fact]
        public void CheckLimits_FlagsOversizedBatches()
        {
            var result = new ArchiMateLlmResult();
            for (int i = 0; i < MutationPolicy.MaxElements + 1; i++)
            {
                result.Elements.Add(new ArchiMateLlmResult.ElementSpec
                {
                    Type = "BusinessActor",
                    Name = "N" + i,
                    Id = IdHelper.EnsureArchiMateId(null)
                });
            }

            var errors = MutationPolicy.CheckLimits(result);
            Assert.Contains(errors, e => e.Contains("Too many elements"));
        }

        [Fact]
        public void CheckLimits_OkForSmallPayload()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec
            {
                Type = "BusinessActor",
                Name = "Customer",
                Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            });
            Assert.Empty(MutationPolicy.CheckLimits(result));
        }

        [Fact]
        public void DestructiveSummary_IncludesCounts()
        {
            var result = new ArchiMateLlmResult();
            result.RemoveElementIds.Add("a");
            result.RemoveDiagramNames.Add("Idea");
            string summary = MutationPolicy.DestructiveSummary(result);
            Assert.Contains("1 element", summary);
            Assert.Contains("1 diagram", summary);
        }

        [Fact]
        public void CheckLimits_FlagsOversizedNames()
        {
            var result = new ArchiMateLlmResult();
            result.Elements.Add(new ArchiMateLlmResult.ElementSpec
            {
                Type = "BusinessActor",
                Name = new string('N', MutationPolicy.MaxNameChars + 1),
                Id = "id-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            });
            var errors = MutationPolicy.CheckLimits(result);
            Assert.Contains(errors, e => e.Contains("name is too long"));
        }

        [Fact]
        public void CheckLimits_FlagsOversizedIds()
        {
            var result = new ArchiMateLlmResult();
            result.RemoveElementIds.Add(new string('a', MutationPolicy.MaxIdChars + 1));
            var errors = MutationPolicy.CheckLimits(result);
            Assert.Contains(errors, e => e.Contains("id is too long"));
        }
    }
}
