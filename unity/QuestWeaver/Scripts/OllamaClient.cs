// Quest Weaver — streaming Ollama client for Unity.
// Talks to a local Ollama server (http://localhost:11434) and streams chat
// tokens back on the main thread. No packages required (UnityWebRequest only).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace QuestWeaver
{
    public class OllamaClient : MonoBehaviour
    {
        [Tooltip("Ollama server base URL")]
        public string baseUrl = "http://localhost:11434";

        [Tooltip("Model tag, e.g. gemma4:e4b or qwen2.5:0.5b")]
        public string model = "qwen2.5:0.5b";

        [Tooltip("Hard cap on generated tokens — dialogue only, keep it tight")]
        public int maxTokens = 150;

        [Tooltip("How long Ollama keeps the model in VRAM after a request")]
        public string keepAlive = "30m";

        public bool IsWarm { get; private set; }

        /// <summary>Load the model into VRAM before anyone needs it (1-token request).</summary>
        public void Warmup(Action onDone = null)
        {
            StartCoroutine(WarmupRoutine(onDone));
        }

        IEnumerator WarmupRoutine(Action onDone)
        {
            string body = "{\"model\":\"" + Esc(model) + "\",\"stream\":false," +
                          "\"keep_alive\":\"" + Esc(keepAlive) + "\"," +
                          "\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]," +
                          "\"options\":{\"num_predict\":1}}";
            using (var req = MakePost("/api/chat", body, new DownloadHandlerBuffer()))
            {
                yield return req.SendWebRequest();
                IsWarm = req.result == UnityWebRequest.Result.Success;
                if (!IsWarm)
                    Debug.LogWarning("[QuestWeaver] Ollama warmup failed: " + req.error +
                                     " — is Ollama running and the model pulled?");
                onDone?.Invoke();
            }
        }

        /// <summary>
        /// Stream a chat completion. onDelta fires per token chunk (main thread),
        /// onComplete fires once with the full text (or null + error message).
        /// Returns the coroutine so callers can stop it.
        /// </summary>
        public Coroutine Chat(string systemPrompt, string userPrompt,
                              Action<string> onDelta, Action<string, string> onComplete)
        {
            return StartCoroutine(ChatRoutine(systemPrompt, userPrompt, onDelta, onComplete));
        }

        IEnumerator ChatRoutine(string systemPrompt, string userPrompt,
                                Action<string> onDelta, Action<string, string> onComplete)
        {
            bool qwen = model.IndexOf("qwen", StringComparison.OrdinalIgnoreCase) >= 0;
            string options = qwen
                ? "{\"num_predict\":" + maxTokens + ",\"temperature\":0.7,\"top_p\":0.8,\"top_k\":20}"
                : "{\"num_predict\":" + maxTokens + ",\"temperature\":1.0,\"top_p\":0.95,\"top_k\":64}";

            string body = "{\"model\":\"" + Esc(model) + "\",\"stream\":true," +
                          "\"keep_alive\":\"" + Esc(keepAlive) + "\"," +
                          "\"options\":" + options + "," +
                          "\"messages\":[" +
                          "{\"role\":\"system\",\"content\":\"" + Esc(systemPrompt) + "\"}," +
                          "{\"role\":\"user\",\"content\":\"" + Esc(userPrompt) + "\"}]}";

            var handler = new NdjsonStreamHandler(onDelta);
            using (var req = MakePost("/api/chat", body, handler))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success && string.IsNullOrEmpty(handler.Text))
                    onComplete?.Invoke(null, req.error ?? "request failed");
                else if (handler.Error != null)
                    onComplete?.Invoke(null, handler.Error);
                else
                    onComplete?.Invoke(handler.Text, null);
            }
        }

        UnityWebRequest MakePost(string path, string json, DownloadHandler handler)
        {
            var req = new UnityWebRequest(baseUrl + path, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = handler;
            req.SetRequestHeader("Content-Type", "application/json");
            return req;
        }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 16);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        // ---- streaming NDJSON parser (Ollama sends one JSON object per line) ----

        [Serializable] class ChunkMsg { public string content; }
        [Serializable] class Chunk { public ChunkMsg message; public bool done; public string error; }

        class NdjsonStreamHandler : DownloadHandlerScript
        {
            readonly Action<string> onDelta;
            readonly StringBuilder full = new StringBuilder();
            string buf = "";
            public string Text => full.ToString();
            public string Error { get; private set; }

            public NdjsonStreamHandler(Action<string> onDelta) : base(new byte[8192])
            {
                this.onDelta = onDelta;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                buf += Encoding.UTF8.GetString(data, 0, dataLength);
                int nl;
                while ((nl = buf.IndexOf('\n')) >= 0)
                {
                    string line = buf.Substring(0, nl).Trim();
                    buf = buf.Substring(nl + 1);
                    if (line.Length == 0) continue;
                    try
                    {
                        var c = JsonUtility.FromJson<Chunk>(line);
                        if (!string.IsNullOrEmpty(c.error)) { Error = c.error; return false; }
                        if (c.message != null && !string.IsNullOrEmpty(c.message.content))
                        {
                            full.Append(c.message.content);
                            onDelta?.Invoke(c.message.content);
                        }
                    }
                    catch { /* partial or non-JSON line — ignore */ }
                }
                return true;
            }
        }
    }
}
