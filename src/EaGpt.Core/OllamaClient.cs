using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace EaGpt
{
    public sealed class OllamaClient
    {
        public const int DefaultPort = 11434;
        public const string DefaultBaseUrl = "http://localhost:11434";
        public const string DefaultModel = "llama3.2";

        private readonly string _baseUrl;
        private readonly string _model;
        private readonly int _timeoutMs;

        public string BaseUrl => _baseUrl;
        public string Model => _model;

        public OllamaClient() : this(DefaultBaseUrl, DefaultModel)
        {
        }

        public OllamaClient(string baseUrl, string? model, int timeoutMs = 180000)
        {
            if (!OllamaEndpoint.TryNormalize(baseUrl, out string normalized, out string error))
            {
                throw new ArgumentException(error, nameof(baseUrl));
            }

            _baseUrl = normalized;
            _model = SanitizeModelName(model);
            _timeoutMs = ClampTimeout(timeoutMs);
        }

        public static string SanitizeModelName(string? model)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                return DefaultModel;
            }

            string t = model!.Trim();
            if (t.Length > 128)
            {
                t = t.Substring(0, 128);
            }

            foreach (char c in t)
            {
                if (c < 32 || c == '"' || c == '\\')
                {
                    return DefaultModel;
                }
            }

            return t;
        }

        public static int ClampTimeout(int timeoutMs)
        {
            if (timeoutMs < 3000)
            {
                return 3000;
            }

            if (timeoutMs > 600000)
            {
                return 600000;
            }

            return timeoutMs;
        }

        private static HttpWebRequest CreateRequest(string url, string method, int timeoutMs)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;
            request.AllowAutoRedirect = false;
            request.MaximumAutomaticRedirections = 0;
            request.KeepAlive = false;
            request.Proxy = BypassProxy.Instance;
            if (request.ServicePoint != null)
            {
                request.ServicePoint.Expect100Continue = false;
            }

            return request;
        }

        /// <summary>
        /// Direct connections only. An HTTP_PROXY would otherwise receive the model digest.
        /// </summary>
        private sealed class BypassProxy : IWebProxy
        {
            public static readonly BypassProxy Instance = new BypassProxy();

            public ICredentials? Credentials { get; set; }

            public Uri? GetProxy(Uri destination)
            {
                return destination;
            }

            public bool IsBypassed(Uri host)
            {
                return true;
            }
        }

        public bool CheckConnection()
        {
            try
            {
                var request = CreateRequest(_baseUrl + "/api/tags", "GET", 3000);
                using var response = (HttpWebResponse)request.GetResponse();
                return (int)response.StatusCode >= 200 && (int)response.StatusCode < 400;
            }
            catch
            {
                return false;
            }
        }

        public IList<string> FetchInstalledModelNames()
        {
            string body = Get(_baseUrl + "/api/tags", 10000);
            var names = OllamaJson.ParseModelNames(body);
            var list = new List<string>(names);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public string Chat(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            return Chat(systemPrompt, userPrompt, null, cancellationToken);
        }

        public string Chat(string systemPrompt, string userPrompt, Action<string>? onDelta, CancellationToken cancellationToken = default)
        {
            string requestBody = BuildChatRequestJson(_model, systemPrompt, userPrompt, stream: onDelta != null);
            var request = CreateRequest(_baseUrl + "/api/chat", "POST", _timeoutMs);
            request.ContentType = "application/json";
            cancellationToken.ThrowIfCancellationRequested();
            using (cancellationToken.Register(() =>
            {
                try
                {
                    request.Abort();
                }
                catch
                {
                    // already completed
                }
            }))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bytes.Length;
                using (Stream os = request.GetRequestStream())
                {
                    os.Write(bytes, 0, bytes.Length);
                }

                using var response = (HttpWebResponse)request.GetResponse();
                using var stream = response.GetResponseStream();
                if (stream == null)
                {
                    return "";
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);
                if (onDelta == null)
                {
                    string body = reader.ReadToEnd();
                    ThrowIfHttpError(response, body);
                    return OllamaJson.ExtractMessageContent(body);
                }

                var full = new StringBuilder();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string delta = OllamaJson.ExtractMessageContent(line);
                    if (delta.Length > 0)
                    {
                        full.Append(delta);
                        onDelta(delta);
                    }
                }

                return full.ToString();
            }
        }

        public static string BuildChatRequestJson(string model, string systemPrompt, string userPrompt, bool stream)
        {
            var sb = new StringBuilder();
            sb.Append("{\"model\":\"").Append(JsonUtil.Escape(model)).Append("\",\"stream\":").Append(stream ? "true" : "false");
            sb.Append(",\"messages\":[");
            sb.Append("{\"role\":\"system\",\"content\":\"").Append(JsonUtil.Escape(systemPrompt)).Append("\"},");
            sb.Append("{\"role\":\"user\",\"content\":\"").Append(JsonUtil.Escape(userPrompt)).Append("\"}");
            sb.Append("]}");
            return sb.ToString();
        }

        private static string Get(string url, int timeoutMs)
        {
            var request = CreateRequest(url, "GET", timeoutMs);
            using var response = (HttpWebResponse)request.GetResponse();
            using var stream = response.GetResponseStream();
            using var reader = stream != null ? new StreamReader(stream, Encoding.UTF8) : null;
            string body = reader != null ? reader.ReadToEnd() : "";
            ThrowIfHttpError(response, body);
            return body;
        }

        private static void ThrowIfHttpError(HttpWebResponse response, string body)
        {
            int code = (int)response.StatusCode;
            if (code >= 400)
            {
                throw new InvalidOperationException("Ollama returned " + code + (string.IsNullOrEmpty(body) ? "" : ": " + body));
            }
        }
    }

    public static class OllamaJson
    {
        public static IList<string> ParseModelNames(string? json)
        {
            var names = new List<string>();
            if (string.IsNullOrEmpty(json))
            {
                return names;
            }

            // "name":"llama3.2:latest"
            int idx = 0;
            while (true)
            {
                int key = json!.IndexOf("\"name\"", idx, StringComparison.Ordinal);
                if (key < 0)
                {
                    break;
                }

                int colon = json.IndexOf(':', key);
                int quote = json.IndexOf('"', colon + 1);
                if (quote < 0)
                {
                    break;
                }

                string name = JsonUtil.ReadJsonString(json, quote + 1);
                if (name.Length > 0 && !names.Contains(name))
                {
                    names.Add(name);
                }

                idx = quote + 1;
            }

            return names;
        }

        public static string ExtractMessageContent(string json)
        {
            int msgStart = json.IndexOf("\"message\"", StringComparison.Ordinal);
            if (msgStart < 0)
            {
                return ExtractField(json, "\"response\":\"");
            }

            int contentKey = json.IndexOf("\"content\":\"", msgStart, StringComparison.Ordinal);
            if (contentKey < 0)
            {
                return "";
            }

            return JsonUtil.ReadJsonString(json, contentKey + "\"content\":\"".Length);
        }

        private static string ExtractField(string json, string key)
        {
            int start = json.IndexOf(key, StringComparison.Ordinal);
            if (start < 0)
            {
                return json.Trim();
            }

            return JsonUtil.ReadJsonString(json, start + key.Length);
        }
    }
}
