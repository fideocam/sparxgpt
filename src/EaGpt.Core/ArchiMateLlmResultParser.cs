using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EaGpt
{
    /// <summary>
    /// Parses ArchiGPT-compatible CHANGES JSON from an LLM reply.
    /// </summary>
    public static class ArchiMateLlmResultParser
    {
        private static readonly Regex ElementBlock = new Regex(
            "\"type\"\\s*:\\s*\"([^\"]*)\"\\s*,\\s*\"name\"\\s*:\\s*\"([^\"]*)\"\\s*,\\s*\"id\"\\s*:\\s*\"([^\"]*)\"",
            RegexOptions.Singleline);

        private static readonly Regex RelationshipBlock = new Regex(
            "\"type\"\\s*:\\s*\"([^\"]*)\"\\s*,\\s*\"source\"\\s*:\\s*\"([^\"]*)\"\\s*,\\s*\"target\"\\s*:\\s*\"([^\"]*)\"",
            RegexOptions.Singleline);

        private static readonly Regex RelName = new Regex("\"name\"\\s*:\\s*\"([^\"]*)\"");
        private static readonly Regex RelId = new Regex("\"id\"\\s*:\\s*\"([^\"]*)\"");
        private static readonly Regex ErrorField = new Regex("\"error\"\\s*:\\s*\"([^\"]*)\"");

        private static readonly Regex ChangesKey = new Regex(
            "\"(elements|removeElementIds|removeRelationshipIds|removeDiagramNames|removeElementFromDiagramIds|removeRelationshipFromDiagramIds)\"\\s*:\\s*\\[|\"diagram\"\\s*:\\s*\\{",
            RegexOptions.Compiled);

        public static bool LooksLikeChangesJson(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (raw!.Length > MutationPolicy.MaxReplyChars)
            {
                return false;
            }

            string json = ExtractJson(raw);
            return ChangesKey.IsMatch(json);
        }

        public static ArchiMateLlmResult Parse(string? rawResponse)
        {
            if (rawResponse != null && rawResponse.Length > MutationPolicy.MaxReplyChars)
            {
                var oversized = new ArchiMateLlmResult();
                oversized.Error = "Reply too large to apply as model changes.";
                return oversized;
            }
            string json = ExtractJson(rawResponse);
            var result = new ArchiMateLlmResult();

            Match err = ErrorField.Match(json);
            if (err.Success)
            {
                result.Error = err.Groups[1].Value.Trim();
            }

            ParseObjectArray(json, "\"elements\"", block =>
            {
                Match m = ElementBlock.Match(block);
                if (!m.Success)
                {
                    return;
                }

                result.Elements.Add(new ArchiMateLlmResult.ElementSpec
                {
                    Type = m.Groups[1].Value.Trim(),
                    Name = JsonUtil.Unescape(m.Groups[2].Value),
                    Id = m.Groups[3].Value.Trim()
                });
            });

            ParseObjectArray(json, "\"relationships\"", block =>
            {
                Match m = RelationshipBlock.Match(block);
                if (!m.Success)
                {
                    return;
                }

                var rel = new ArchiMateLlmResult.RelationshipSpec
                {
                    Type = m.Groups[1].Value.Trim(),
                    Source = m.Groups[2].Value.Trim(),
                    Target = m.Groups[3].Value.Trim()
                };
                Match nameM = RelName.Match(block);
                rel.Name = nameM.Success ? JsonUtil.Unescape(nameM.Groups[1].Value) : "";
                Match idM = RelId.Match(block);
                rel.Id = idM.Success ? idM.Groups[1].Value.Trim() : null;
                result.Relationships.Add(rel);
            });

            var diagramKey = new Regex("\"diagram\"\\s*:\\s*\\{");
            Match diagramMatch = diagramKey.Match(json);
            if (diagramMatch.Success)
            {
                int objStart = diagramMatch.Index + diagramMatch.Length - 1;
                int objEnd = JsonUtil.FindMatchingBracket(json, objStart);
                if (objEnd > objStart)
                {
                    result.Diagram = ParseDiagram(json.Substring(objStart, objEnd - objStart + 1));
                }
            }

            ParseStringArray(json, "\"removeElementIds\"", result.RemoveElementIds);
            ParseStringArray(json, "\"removeRelationshipIds\"", result.RemoveRelationshipIds);
            ParseStringArray(json, "\"removeDiagramNames\"", result.RemoveDiagramNames);
            ParseStringArray(json, "\"removeElementFromDiagramIds\"", result.RemoveElementFromDiagramIds);
            ParseStringArray(json, "\"removeRelationshipFromDiagramIds\"", result.RemoveRelationshipFromDiagramIds);

            return result;
        }

        private static ArchiMateLlmResult.DiagramSpec ParseDiagram(string diagramStr)
        {
            var diagram = new ArchiMateLlmResult.DiagramSpec();
            Match nameM = new Regex("\"name\"\\s*:\\s*\"([^\"]*)\"").Match(diagramStr);
            if (nameM.Success)
            {
                diagram.Name = JsonUtil.Unescape(nameM.Groups[1].Value.Trim());
            }

            Match vpM = new Regex("\"viewpoint\"\\s*:\\s*\"([^\"]*)\"").Match(diagramStr);
            if (vpM.Success)
            {
                diagram.Viewpoint = JsonUtil.Unescape(vpM.Groups[1].Value.Trim());
            }

            ParseObjectArray(diagramStr, "\"nodes\"", block =>
            {
                Match idM = new Regex("\"elementId\"\\s*:\\s*\"([^\"]*)\"").Match(block);
                if (!idM.Success)
                {
                    return;
                }

                diagram.Nodes.Add(new ArchiMateLlmResult.DiagramNodeSpec
                {
                    ElementId = idM.Groups[1].Value.Trim(),
                    X = ParseCoord(block, "x", 0),
                    Y = ParseCoord(block, "y", 0),
                    Width = ParseCoord(block, "width", 120),
                    Height = ParseCoord(block, "height", 55)
                });
            });

            ParseObjectArray(diagramStr, "\"connections\"", block =>
            {
                Match src = new Regex("\"sourceElementId\"\\s*:\\s*\"([^\"]*)\"").Match(block);
                Match tgt = new Regex("\"targetElementId\"\\s*:\\s*\"([^\"]*)\"").Match(block);
                if (!src.Success || !tgt.Success)
                {
                    return;
                }

                Match rel = new Regex("\"relationshipId\"\\s*:\\s*\"([^\"]*)\"").Match(block);
                diagram.Connections.Add(new ArchiMateLlmResult.DiagramConnectionSpec
                {
                    SourceElementId = src.Groups[1].Value.Trim(),
                    TargetElementId = tgt.Groups[1].Value.Trim(),
                    RelationshipId = rel.Success ? rel.Groups[1].Value.Trim() : null
                });
            });

            return diagram;
        }

        private static void ParseObjectArray(string json, string key, System.Action<string> onObject)
        {
            int start = json.IndexOf(key);
            if (start < 0)
            {
                return;
            }

            int arrayStart = json.IndexOf('[', start);
            int arrayEnd = JsonUtil.FindMatchingBracket(json, arrayStart);
            if (arrayEnd <= arrayStart)
            {
                return;
            }

            string inner = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
            foreach (var range in FindObjectRanges(inner))
            {
                onObject(inner.Substring(range.Start, range.Length));
            }
        }

        private static void ParseStringArray(string json, string key, List<string> output)
        {
            int start = json.IndexOf(key);
            if (start < 0)
            {
                return;
            }

            int arrayStart = json.IndexOf('[', start);
            int arrayEnd = JsonUtil.FindMatchingBracket(json, arrayStart);
            if (arrayEnd <= arrayStart)
            {
                return;
            }

            string arr = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
            foreach (Match m in Regex.Matches(arr, "\"([^\"]+)\""))
            {
                string id = m.Groups[1].Value.Trim();
                if (id.Length > 0)
                {
                    output.Add(id);
                }
            }
        }

        private static int ParseCoord(string block, string key, int defaultValue)
        {
            Match m = Regex.Match(block, "\"" + key + "\"\\s*:\\s*(-?\\d{1,6})");
            if (!m.Success || !int.TryParse(m.Groups[1].Value, out int n))
            {
                return defaultValue;
            }

            if (n < 0)
            {
                return 0;
            }

            return n > MutationPolicy.MaxCoord ? MutationPolicy.MaxCoord : n;
        }

        private static string ExtractJson(string? raw)
        {
            if (raw == null)
            {
                return "{}";
            }

            string s = raw.Trim();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '{')
                {
                    continue;
                }

                int end = JsonUtil.FindMatchingBracket(s, i);
                if (end <= i)
                {
                    continue;
                }

                string candidate = s.Substring(i, end - i + 1);
                if (candidate.Contains("\"elements\"") || candidate.Contains("\"diagram\"") ||
                    candidate.Contains("\"removeElementIds\"") || candidate.Contains("\"removeDiagramNames\"") ||
                    candidate.Contains("\"removeElementFromDiagramIds\""))
                {
                    return candidate;
                }
            }

            return s;
        }

        private static List<(int Start, int Length)> FindObjectRanges(string str)
        {
            var list = new List<(int, int)>();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != '{')
                {
                    continue;
                }

                int end = JsonUtil.FindMatchingBracket(str, i);
                if (end > i)
                {
                    list.Add((i, end - i + 1));
                    i = end;
                }
            }

            return list;
        }
    }
}
