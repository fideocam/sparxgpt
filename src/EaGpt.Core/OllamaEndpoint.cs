using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace EaGpt
{
    /// <summary>
    /// Validates the Ollama base URL so the add-in cannot be pointed at
    /// non-HTTP schemes, credential-bearing URLs, or well-known cloud metadata endpoints.
    /// LAN and localhost remain allowed (same as ArchiGPT).
    /// </summary>
    public static class OllamaEndpoint
    {
        public const int MaxUrlLength = 2048;

        public static bool TryNormalize(string? raw, out string normalized, out string error)
        {
            normalized = OllamaClient.DefaultBaseUrl;
            error = "";
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Ollama URL is empty.";
                return false;
            }

            string s = raw!.Trim();
            if (s.Length > MaxUrlLength)
            {
                error = "Ollama URL is too long.";
                return false;
            }

            if (s.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                s = "http://" + s;
            }

            if (!Uri.TryCreate(s, UriKind.Absolute, out Uri? uri) || uri == null)
            {
                error = "Ollama URL is not a valid absolute URI.";
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                error = "Ollama URL must be http or https.";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                error = "Ollama URL must not contain credentials.";
                return false;
            }

            if (string.IsNullOrEmpty(uri.Host))
            {
                error = "Ollama URL must include a host.";
                return false;
            }

            if (IsBlockedHost(uri.Host))
            {
                error = "Ollama URL host is not allowed.";
                return false;
            }

            var builder = new UriBuilder(uri.Scheme, uri.Host)
            {
                Port = uri.IsDefaultPort ? -1 : uri.Port,
                Path = "",
                Query = "",
                Fragment = ""
            };
            normalized = builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            return true;
        }

        public static string NormalizeOrDefault(string? raw)
        {
            return TryNormalize(raw, out string normalized, out _) ? normalized : OllamaClient.DefaultBaseUrl;
        }

        public static bool IsBlockedHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return true;
            }

            string h = StripHost(host);
            if (h.Equals("169.254.169.254", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("metadata", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("instance-data", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("100.100.100.200", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (TryParseHostAsIp(h, out IPAddress? ip) && ip != null)
            {
                return IsBlockedAddress(ip);
            }

            return false;
        }

        internal static string StripHost(string host)
        {
            string h = host.Trim();
            int zone = h.IndexOf('%');
            if (zone >= 0)
            {
                h = h.Substring(0, zone);
            }

            h = h.Trim('[', ']');
            while (h.EndsWith(".", StringComparison.Ordinal))
            {
                h = h.Substring(0, h.Length - 1);
            }

            return h;
        }

        internal static bool TryParseHostAsIp(string host, out IPAddress? ip)
        {
            ip = null;
            string h = StripHost(host);
            if (h.Length == 0)
            {
                return false;
            }

            if (IPAddress.TryParse(h, out IPAddress? parsed) && parsed != null)
            {
                ip = parsed;
                return true;
            }

            if (TryParseDwordIpv4(h, out ip))
            {
                return true;
            }

            string[] parts = h.Split('.');
            if (parts.Length == 4)
            {
                var bytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    if (!TryParseIpv4Octet(parts[i], out bytes[i]))
                    {
                        return false;
                    }
                }

                ip = new IPAddress(bytes);
                return true;
            }

            return false;
        }

        internal static bool IsBlockedAddress(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = ip.GetAddressBytes();
                if (b.Length == 4 && b[0] == 169 && b[1] == 254)
                {
                    return true;
                }

                if (b.Length == 4 && b[0] == 100 && b[1] == 100 && b[2] == 100 && b[3] == 200)
                {
                    return true;
                }
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // AWS IMDSv2 IPv6 is fd00:ec2::254 (last group is hex, not decimal 254).
                if (IPAddress.TryParse("fd00:ec2::254", out IPAddress? awsImds) && awsImds != null && ip.Equals(awsImds))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseDwordIpv4(string host, out IPAddress? ip)
        {
            ip = null;
            ulong dword;
            if (host.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (!ulong.TryParse(host.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out dword))
                {
                    return false;
                }
            }
            else if (host.StartsWith("0", StringComparison.Ordinal) && host.Length > 1 &&
                     ulong.TryParse(host, NumberStyles.Integer, CultureInfo.InvariantCulture, out dword))
            {
                // leading-zero dword: treat as decimal still; octal dotted form is handled per-octet
            }
            else if (!ulong.TryParse(host, NumberStyles.Integer, CultureInfo.InvariantCulture, out dword))
            {
                return false;
            }

            if (dword > 0xFFFFFFFFUL)
            {
                return false;
            }

            ip = new IPAddress(new byte[]
            {
                (byte)((dword >> 24) & 0xFF),
                (byte)((dword >> 16) & 0xFF),
                (byte)((dword >> 8) & 0xFF),
                (byte)(dword & 0xFF)
            });
            return true;
        }

        private static bool TryParseIpv4Octet(string raw, out byte value)
        {
            value = 0;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            int n;
            if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(raw.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out n))
                {
                    return false;
                }
            }
            else if (raw.Length > 1 && raw.StartsWith("0", StringComparison.Ordinal))
            {
                try
                {
                    n = Convert.ToInt32(raw, 8);
                }
                catch
                {
                    return false;
                }
            }
            else if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
            {
                return false;
            }

            if (n < 0 || n > 255)
            {
                return false;
            }

            value = (byte)n;
            return true;
        }
    }
}
