using System.IO;
using System.Reflection;
using System.Text;

namespace EaGpt
{
    public static class ArchiMateSystemPrompt
    {
        private static readonly object Gate = new object();
        private static string? _loaded;

        public static string GetSystemPrompt()
        {
            if (_loaded != null)
            {
                return _loaded;
            }

            lock (Gate)
            {
                if (_loaded != null)
                {
                    return _loaded;
                }

                try
                {
                    var asm = typeof(ArchiMateSystemPrompt).Assembly;
                    foreach (string name in asm.GetManifestResourceNames())
                    {
                        if (name.EndsWith("system-prompt.txt"))
                        {
                            using Stream? stream = asm.GetManifestResourceStream(name);
                            if (stream != null)
                            {
                                using var reader = new StreamReader(stream, Encoding.UTF8);
                                string content = reader.ReadToEnd().Trim();
                                if (content.Length > 0)
                                {
                                    _loaded = content;
                                    return _loaded;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // fall through to default
                }

                _loaded = DefaultPrompt;
                return _loaded;
            }
        }

        internal const string DefaultPrompt =
            "You are an expert in the Open Group ArchiMate 3.2 specification. " +
            "Respond in one of two ways:\n\n" +
            "1) ANALYSIS: For analysis, description, or review, respond with plain text only. " +
            "Only describe elements and relationships that appear in the supplied model XML.\n\n" +
            "2) CHANGES: For add/create/generate/remove, respond ONLY with a single JSON object: " +
            "{\"elements\":[{\"type\":\" \",\"name\":\" \",\"id\":\" \"}]," +
            "\"relationships\":[{\"type\":\" \",\"source\":\" \",\"target\":\" \",\"name\":\" \",\"id\":\" \"}]}. " +
            "Use ArchiMate ids: id- plus 32 hex. Optional removeElementIds, removeRelationshipIds, " +
            "removeDiagramNames, removeElementFromDiagramIds, removeRelationshipFromDiagramIds, and diagram.";
    }
}
