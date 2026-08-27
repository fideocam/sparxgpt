using System;
using System.Text;

namespace EaGpt
{
    internal static class JsonUtil
    {
        public static string Escape(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            var sb = new StringBuilder(s!.Length + 8);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            return sb.ToString();
        }

        public static string Unescape(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            return s!
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        public static int FindMatchingBracket(string s, int openIndex)
        {
            if (openIndex < 0 || openIndex >= s.Length)
            {
                return -1;
            }

            char open = s[openIndex];
            char close = open == '[' ? ']' : '}';
            int depth = 1;
            bool inString = false;
            bool escape = false;
            for (int i = openIndex + 1; i < s.Length; i++)
            {
                char c = s[i];
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escape = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                {
                    continue;
                }

                if (c == open)
                {
                    depth++;
                }
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Extract a JSON string value that starts at <paramref name="contentStart"/>
        /// (the character after the opening quote).
        /// </summary>
        public static string ReadJsonString(string json, int contentStart)
        {
            var sb = new StringBuilder();
            for (int i = contentStart; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    char next = json[i + 1];
                    i++;
                    switch (next)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default: sb.Append(next); break;
                    }
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
