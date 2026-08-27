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

        private LlmApiKind _apiKind = LlmApiKind.Unknown;

        public string BaseUrl => _baseUrl;
        public string Model => _model;

        public string ApiDisplayName =>
            _apiKind == LlmApiKind.OpenAiCompat ? "OpenAI-compatible (LM Studio)" :
            _apiKind == LlmApiKind.Ollama ? "Ollama" : "LLM";

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
                DetectApi();
                return _apiKind != LlmApiKind.Unknown;
            }
            catch
            {
                return false;
            }
        }

        public IList<string> FetchInstalledModelNames()
        {
            DetectApi();
            if (_apiKind == LlmApiKind.OpenAiCompat)
            {
                string body = Get(_baseUrl + "/v1/models", 10000);
                var names = OllamaJson.ParseOpenAiModelIds(body);
                var list = new List<string>(names);
                list.Sort(StringComparer.OrdinalIgnoreCase);
                return list;
            }

            string tags = Get(_baseUrl + "/api/tags", 10000);
            var ollama = new List<string>(OllamaJson.ParseModelNames(tags));
            ollama.Sort(StringComparer.OrdinalIgnoreCase);
            return ollama;
        }

        public string Chat(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            return Chat(systemPrompt, userPrompt, null, cancellationToken, null);
        }

        public string Chat(string systemPrompt, string userPrompt, Action<string>? onDelta, CancellationToken cancellationToken = default)
        {
            return Chat(systemPrompt, userPrompt, onDelta, cancellationToken, null);
        }

        public string Chat(
            string systemPrompt,
            string userPrompt,
            Action<string>? onDelta,
            CancellationToken cancellationToken,
            IList<ChatTurn>? history)
        {
            DetectApi();
            bool openAi = _apiKind == LlmApiKind.OpenAiCompat;
            string path = openAi ? "/v1/chat/completions" : "/api/chat";
            string requestBody = BuildChatRequestJson(_model, systemPrompt, userPrompt, stream: onDelta != null, history);
            var request = CreateRequest(_baseUrl + path, "POST", _timeoutMs);
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
                    return OllamaJson.ExtractChatDelta(body);
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

                    string delta = OllamaJson.ExtractChatDelta(line);
                    if (delta.Length > 0)
                    {
                        full.Append(delta);
                        onDelta(delta);
                    }
                }

                return full.ToString();
            }
        }

        internal void DetectApi()
        {
            if (_apiKind != LlmApiKind.Unknown)
            {
                return;
            }

            bool preferOpenAi = false;
            try
            {
                preferOpenAi = new Uri(_baseUrl).Port == 1234;
            }
            catch
            {
                // keep Ollama-first
            }

            if (preferOpenAi)
            {
                if (TryGet("/v1/models", 3000, out _))
                {
                    _apiKind = LlmApiKind.OpenAiCompat;
                    return;
                }

                if (TryGet("/api/tags", 3000, out _))
                {
                    _apiKind = LlmApiKind.Ollama;
                    return;
                }
            }
            else
            {
                if (TryGet("/api/tags", 3000, out _))
                {
                    _apiKind = LlmApiKind.Ollama;
                    return;
                }

                if (TryGet("/v1/models", 3000, out _))
                {
                    _apiKind = LlmApiKind.OpenAiCompat;
                    return;
                }
            }

            _apiKind = LlmApiKind.Unknown;
        }

        private bool TryGet(string path, int timeoutMs, out string body)
        {
            body = "";
            try
            {
                body = Get(_baseUrl + path, timeoutMs);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string BuildChatRequestJson(string model, string systemPrompt, string userPrompt, bool stream)
        {
            return BuildChatRequestJson(model, systemPrompt, userPrompt, stream, null);
        }

        public static string BuildChatRequestJson(
            string model,
            string systemPrompt,
            string userPrompt,
            bool stream,
            IList<ChatTurn>? history)
        {
            var sb = new StringBuilder();
            sb.Append("{\"model\":\"").Append(JsonUtil.Escape(model)).Append("\",\"stream\":").Append(stream ? "true" : "false");
            sb.Append(",\"messages\":[");
            sb.Append("{\"role\":\"system\",\"content\":\"").Append(JsonUtil.Escape(systemPrompt)).Append("\"}");
            if (history != null)
            {
                foreach (var turn in history)
                {
                    if (turn == null || string.IsNullOrWhiteSpace(turn.User))
                    {
                        continue;
                    }

                    sb.Append(",{\"role\":\"user\",\"content\":\"").Append(JsonUtil.Escape(turn.User)).Append("\"}");
                    if (!string.IsNullOrWhiteSpace(turn.Assistant))
                    {
                        sb.Append(",{\"role\":\"assistant\",\"content\":\"").Append(JsonUtil.Escape(turn.Assistant)).Append("\"}");
                    }
                }
            }

            sb.Append(",{\"role\":\"user\",\"content\":\"").Append(JsonUtil.Escape(userPrompt)).Append("\"}");
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
            return ExtractChatDelta(json);
        }

        /// <summary>
        /// Content from an Ollama /api/chat line or an OpenAI-compatible /v1/chat/completions payload (including SSE).
        /// </summary>
        public static string ExtractChatDelta(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return "";
            }

            string s = json.Trim();
            if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(5).Trim();
            }

            if (s == "[DONE]")
            {
                return "";
            }

            int delta = s.IndexOf("\"delta\"", StringComparison.Ordinal);
            if (delta >= 0)
            {
                int contentKey = s.IndexOf("\"content\":\"", delta, StringComparison.Ordinal);
                if (contentKey >= 0)
                {
                    return JsonUtil.ReadJsonString(s, contentKey + "\"content\":\"".Length);
                }
            }

            int msgStart = s.IndexOf("\"message\"", StringComparison.Ordinal);
            if (msgStart >= 0)
            {
                int contentKey = s.IndexOf("\"content\":\"", msgStart, StringComparison.Ordinal);
                if (contentKey >= 0)
                {
                    return JsonUtil.ReadJsonString(s, contentKey + "\"content\":\"".Length);
                }
            }

            int responseKey = s.IndexOf("\"response\":\"", StringComparison.Ordinal);
            if (responseKey >= 0)
            {
                return JsonUtil.ReadJsonString(s, responseKey + "\"response\":\"".Length);
            }

            return "";
        }

        public static IList<string> ParseOpenAiModelIds(string? json)
        {
            var names = new List<string>();
            if (string.IsNullOrEmpty(json))
            {
                return names;
            }

            int idx = 0;
            while (true)
            {
                int key = json!.IndexOf("\"id\"", idx, StringComparison.Ordinal);
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

    internal enum LlmApiKind
    {
        Unknown = 0,
        Ollama = 1,
        OpenAiCompat = 2
    }
}
