using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class IdHelperTests
    {
        [Fact]
        public void Ensure_KeepsValidArchiId()
        {
            const string id = "id-a1b2c3d4e5f67890abcdef1234567890";
            Assert.Equal(id, IdHelper.EnsureArchiMateId(id));
            Assert.True(IdHelper.IsArchiMateId(id));
        }

        [Fact]
        public void FromEaGuid_StripsBracesAndHyphens()
        {
            string id = IdHelper.FromEaGuid("{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}");
            Assert.Equal("id-a1b2c3d4e5f67890abcdef1234567890", id);
        }

        [Fact]
        public void ToEaGuid_RoundTrip()
        {
            const string id = "id-a1b2c3d4e5f67890abcdef1234567890";
            Assert.Equal("{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}", IdHelper.ToEaGuid(id));
        }

        [Fact]
        public void Ensure_GeneratesWhenMissing()
        {
            string id = IdHelper.EnsureArchiMateId(null);
            Assert.True(IdHelper.IsArchiMateId(id));
        }

        [Fact]
        public void Ensure_DoesNotPassThroughArbitraryStrings()
        {
            string id = IdHelper.EnsureArchiMateId("http://evil.example/;DROP");
            Assert.True(IdHelper.IsArchiMateId(id));
            Assert.DoesNotContain("evil", id);
            Assert.DoesNotContain("DROP", id);
        }
    }
}
