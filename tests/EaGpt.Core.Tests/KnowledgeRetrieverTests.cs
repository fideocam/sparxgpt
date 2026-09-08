using System;
using System.IO;
using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class KnowledgeRetrieverTests
    {
        [Fact]
        public void Retrieve_EmptyFolder_ReturnsEmpty()
        {
            string dir = Path.Combine(Path.GetTempPath(), "eagpt-knowledge-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            Assert.Equal("", KnowledgeRetriever.Retrieve(dir, "deployment viewpoint"));
        }

        [Fact]
        public void Retrieve_PicksMatchingCollection()
        {
            string dir = Path.Combine(Path.GetTempPath(), "eagpt-knowledge-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "principles"));
            Directory.CreateDirectory(Path.Combine(dir, "cmdb"));
            File.WriteAllText(Path.Combine(dir, "principles", "naming.md"), "Applications are named APP-xxx. Prefer reuse of existing components.");
            File.WriteAllText(Path.Combine(dir, "cmdb", "servers.csv"), "ci,class,name\n1,server,db01");
            string text = KnowledgeRetriever.Retrieve(dir, "How should applications be named APP-xxx?", maxChars: 4000);
            Assert.Contains("COMPANY KNOWLEDGE", text);
            Assert.Contains("principles/naming.md", text);
            Assert.Contains("APP-xxx", text);
            Assert.DoesNotContain("db01", text);
        }

        [Fact]
        public void Retrieve_NoQuery_IncludesFiles()
        {
            string dir = Path.Combine(Path.GetTempPath(), "eagpt-knowledge-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "note.txt"), "Tiedonhallintamalli requires a data store inventory.");
            string text = KnowledgeRetriever.Retrieve(dir, "", maxChars: 2000);
            Assert.Contains("Tiedonhallintamalli", text);
        }

        [Fact]
        public void UserMessage_IncludesKnowledgeAfterModel()
        {
            string msg = UserMessageBuilder.BuildUserMessage("sel", "<archimate/>", "Create a technology diagram", "--- COMPANY KNOWLEDGE ---\nUse Node for servers.\n");
            int xml = msg.IndexOf("<archimate/>", StringComparison.Ordinal);
            int know = msg.IndexOf("COMPANY KNOWLEDGE", StringComparison.Ordinal);
            int req = msg.IndexOf("User request:", StringComparison.Ordinal);
            Assert.True(xml >= 0 && know > xml && req > know);
        }

        [Fact]
        public void Retrieve_DefaultPack_FinnishQuery_HitsTiedonhallintaFiles()
        {
            string? folder = FindRepoKnowledge();
            if (folder == null)
            {
                return;
            }

            string text = KnowledgeRetriever.Retrieve(folder, "Kuvaa tiedonhallintamalli tietovarannot ja tekninen rajapinta", 8000);
            Assert.Contains("COMPANY KNOWLEDGE", text);
            Assert.Contains("tiedonhallintamalli/", text);
            Assert.Contains("tietovaranto", text, StringComparison.OrdinalIgnoreCase);
        }

        private static string? FindRepoKnowledge()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, "knowledge");
                if (File.Exists(Path.Combine(candidate, "tiedonhallintamalli", "overview.md")))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}
