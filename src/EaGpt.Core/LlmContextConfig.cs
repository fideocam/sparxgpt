using System;
using System.Collections.Generic;

namespace EaGpt
{
    /// <summary>
    /// Ollama num_ctx / XML budget, aligned with ArchiGPT LlmContextConfig.
    /// </summary>
    public static class LlmContextConfig
    {
        public const int DefaultReportedCtxCap = 32768;
        public const int DefaultNumCtx = 65536;
        public const int MinNumCtx = 2048;
        public const int MaxNumCtx = 2 * 1024 * 1024;
        public const int DefaultTimeoutMs = 120000;
        public const int TimeoutCeilingMs = 7_200_000;
        public const int DefaultMaxXmlChars = 36000;
        public const int MaxXmlCharsCeiling = 500000;
        public const int MinAutoXmlChars = 6000;
        public const int ReplyReserveTokens = 8192;

        public static int ClampNumCtx(int value)
        {
            if (value < MinNumCtx)
            {
                return MinNumCtx;
            }

            return value > MaxNumCtx ? MaxNumCtx : value;
        }

        public static int ResolveNumCtx(int reportedByOllama, int uiNumCtx, bool useModelMax)
        {
            if (useModelMax)
            {
                if (reportedByOllama >= MinNumCtx)
                {
                    return Math.Min(reportedByOllama, MaxNumCtx);
                }

                if (uiNumCtx >= MinNumCtx)
                {
                    return Math.Min(uiNumCtx, MaxNumCtx);
                }

                return Math.Min(DefaultNumCtx, MaxNumCtx);
            }

            if (uiNumCtx >= MinNumCtx)
            {
                if (reportedByOllama >= MinNumCtx)
                {
                    return Math.Min(Math.Min(uiNumCtx, reportedByOllama), MaxNumCtx);
                }

                return Math.Min(uiNumCtx, MaxNumCtx);
            }

            if (reportedByOllama >= MinNumCtx)
            {
                return Math.Min(Math.Min(reportedByOllama, MaxNumCtx), DefaultReportedCtxCap);
            }

            return Math.Min(DefaultNumCtx, DefaultReportedCtxCap);
        }

        public static int ResolveReadTimeoutMs(int numCtx, int explicitTimeoutMs)
        {
            int floor = explicitTimeoutMs > 0 ? OllamaClient.ClampTimeout(explicitTimeoutMs) : DefaultTimeoutMs;
            if (numCtx <= DefaultReportedCtxCap)
            {
                return floor;
            }

            long scaled = (long)numCtx * DefaultTimeoutMs / DefaultReportedCtxCap;
            if (scaled > TimeoutCeilingMs)
            {
                scaled = TimeoutCeilingMs;
            }

            return (int)Math.Max(floor, scaled);
        }

        public static int ResolveMaxXmlChars(int numCtx, int systemPromptChars, int userOverheadChars)
        {
            if (numCtx <= 0)
            {
                numCtx = DefaultNumCtx;
            }

            int systemTok = Math.Max(1, (systemPromptChars + 3) / 4);
            int overheadTok = Math.Max(1, (userOverheadChars + 3) / 4);
            int availTok = numCtx - ReplyReserveTokens - systemTok - overheadTok;
            if (availTok < 256)
            {
                availTok = 256;
            }

            int chars = availTok * 3;
            if (chars < MinAutoXmlChars)
            {
                chars = MinAutoXmlChars;
            }

            return chars > MaxXmlCharsCeiling ? MaxXmlCharsCeiling : chars;
        }
    }

    /// <summary>
    /// Model dropdown helpers: list only names the server reported (ArchiGPT OllamaModelList).
    /// </summary>
    public static class OllamaModelList
    {
        public static List<string> FromServerNames(IList<string>? names)
        {
            var merged = new List<string>();
            if (names != null)
            {
                foreach (string n in names)
                {
                    if (string.IsNullOrWhiteSpace(n))
                    {
                        continue;
                    }

                    string t = n.Trim();
                    bool seen = false;
                    foreach (string e in merged)
                    {
                        if (string.Equals(e, t, StringComparison.OrdinalIgnoreCase))
                        {
                            seen = true;
                            break;
                        }
                    }

                    if (!seen)
                    {
                        merged.Add(t);
                    }
                }
            }

            merged.Sort(StringComparer.OrdinalIgnoreCase);
            return merged;
        }

        public static int IndexOfModel(IList<string>? items, string? name)
        {
            if (items == null || string.IsNullOrEmpty(name))
            {
                return -1;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i], name, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            string latest = name + ":latest";
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i], latest, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(items[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public static string SelectionAfterRefresh(IList<string>? serverItems, string? previous)
        {
            var items = FromServerNames(serverItems);
            int idx = IndexOfModel(items, previous);
            if (idx >= 0)
            {
                return items[idx];
            }

            return items.Count > 0 ? items[0] : "";
        }
    }
}
