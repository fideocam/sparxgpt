using System;
using System.Text.RegularExpressions;

namespace EaGpt
{
    public static class IdHelper
    {
        private static readonly Regex ArchiMateId = new Regex(@"^id-[0-9a-fA-F]{32}$", RegexOptions.Compiled);
        private static readonly Regex Hex32 = new Regex(@"^[0-9a-fA-F]{32}$", RegexOptions.Compiled);

        /// <summary>
        /// Normalize an LLM or EA identifier to ArchiMate form <c>id-</c> + 32 hex.
        /// Accepts existing Archi ids, hyphenated GUIDs, and brace GUIDs.
        /// </summary>
        public static string EnsureArchiMateId(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                string trimmed = id!.Trim();
                if (ArchiMateId.IsMatch(trimmed))
                {
                    return trimmed;
                }

                string hex = StripToHex(trimmed);
                if (hex.Length == 32 && Hex32.IsMatch(hex))
                {
                    return "id-" + hex.ToLowerInvariant();
                }
            }

            return "id-" + Guid.NewGuid().ToString("N");
        }

        public static bool IsArchiMateId(string? id)
        {
            return !string.IsNullOrEmpty(id) && ArchiMateId.IsMatch(id!.Trim());
        }

        /// <summary>
        /// Convert an EA GUID such as <c>{A1B2...}</c> into ArchiMate <c>id-a1b2...</c>.
        /// </summary>
        public static string FromEaGuid(string? eaGuid)
        {
            return EnsureArchiMateId(eaGuid);
        }

        /// <summary>
        /// Convert <c>id-</c> + 32 hex back to an EA-style brace GUID when possible.
        /// </summary>
        public static string ToEaGuid(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "";
            }

            string hex = StripToHex(id!);
            if (hex.Length != 32)
            {
                return id!.Trim();
            }

            hex = hex.ToUpperInvariant();
            return "{" + hex.Substring(0, 8) + "-" + hex.Substring(8, 4) + "-" + hex.Substring(12, 4) + "-" +
                   hex.Substring(16, 4) + "-" + hex.Substring(20, 12) + "}";
        }

        private static string StripToHex(string id)
        {
            string s = id.Trim();
            if (s.StartsWith("id-", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(3);
            }

            s = s.Replace("{", "").Replace("}", "").Replace("-", "");
            return s;
        }
    }
}
