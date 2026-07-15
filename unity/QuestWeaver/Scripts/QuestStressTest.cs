// Quest Weaver — in-game LLM stress test. Press F5 in Play mode.
// Generates fresh dialogue for every QuestNpc in the scene, `rounds` times,
// sequentially (matching how Ollama queues requests), and reports per run:
//   TTFT  time to first token — what the player would feel without prefetch
//   total wall time for the whole line
//   tok   number of streamed chunks (≈ tokens)
//   tok/s generation throughput
// Round 1 includes prompt evaluation; later rounds show Ollama's prefix cache.
// Results draw on screen (OnGUI) and log to the console.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuestWeaver
{
    public class QuestStressTest : MonoBehaviour
    {
        public OllamaClient ollama;
        [Tooltip("How many times to run the full NPC roster")]
        public int rounds = 3;
        public KeyCode hotkey = KeyCode.F5;

        struct Result
        {
            public string npc; public int round;
            public float ttftMs, totalS; public int tokens;
            public float TokPerSec => totalS > 0 ? tokens / totalS : 0;
        }

        readonly List<Result> results = new List<Result>();
        bool running;
        string status = "";

        void Update()
        {
            if (Input.GetKeyDown(hotkey) && !running)
                StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            running = true;
            results.Clear();

#if UNITY_2023_1_OR_NEWER
            var npcs = Object.FindObjectsByType<QuestNpc>(FindObjectsSortMode.None);
            var player = Object.FindFirstObjectByType<QuestPlayer>();
#else
            var npcs = Object.FindObjectsOfType<QuestNpc>();
            var player = Object.FindObjectOfType<QuestPlayer>();
#endif
            if (npcs.Length == 0 || player == null || ollama == null)
            { status = "stress test: need NPCs, a QuestPlayer and an OllamaClient"; running = false; yield break; }

            status = "warming model…";
            if (!ollama.IsWarm)
            {
                bool warmed = false;
                ollama.Warmup(() => warmed = true);
                while (!warmed) yield return null;
            }

            for (int r = 1; r <= rounds; r++)
            {
                foreach (var npc in npcs)
                {
                    status = $"round {r}/{rounds} — {npc.npcName}…";
                    float t0 = Time.realtimeSinceStartup;
                    float ttft = -1f; int toks = 0; bool done = false; string error = null;

                    ollama.Chat(npc.SystemPrompt(), QuestPrompt.User(player.profile),
                        delta => { if (ttft < 0) ttft = (Time.realtimeSinceStartup - t0) * 1000f; toks++; },
                        (full, err) => { error = err; done = true; });

                    while (!done) yield return null;
                    float total = Time.realtimeSinceStartup - t0;

                    if (error != null) { status = "FAILED: " + error; running = false; yield break; }
                    var res = new Result { npc = npc.npcName, round = r, ttftMs = ttft, totalS = total, tokens = toks };
                    results.Add(res);
                    Debug.Log($"[StressTest] {res.npc} r{r}: TTFT {res.ttftMs:F0}ms, total {res.totalS:F2}s, {res.tokens} tok, {res.TokPerSec:F1} tok/s");
                }
            }
            status = "done — " + Summary();
            Debug.Log("[StressTest] " + Summary());
            running = false;
        }

        string Summary()
        {
            if (results.Count == 0) return "no results";
            float ttft = 0, total = 0, tps = 0;
            foreach (var r in results) { ttft += r.ttftMs; total += r.totalS; tps += r.TokPerSec; }
            int n = results.Count;
            return $"{n} generations · avg TTFT {ttft / n:F0}ms · avg total {total / n:F2}s · avg {tps / n:F1} tok/s ({ollama.model})";
        }

        void OnGUI()
        {
            if (!running && results.Count == 0)
            {   // idle: just a hint, no panel
                GUI.Label(new Rect(10, 10, 400, 24), $"[{hotkey}] LLM stress test", Rich(13));
                return;
            }
            const int w = 560;
            GUILayout.BeginArea(new Rect(10, 10, w, Screen.height - 20), GUI.skin.box);
            GUILayout.Label($"<b>LLM stress test</b> — press {hotkey} to run ({ollama != null ? ollama.model : "?"})",
                Rich(14));
            if (!string.IsNullOrEmpty(status)) GUILayout.Label(status, Rich(12));

            if (results.Count > 0)
            {
                GUILayout.Label("npc                round   TTFT ms   total s   tok   tok/s", Mono());
                foreach (var r in results)
                    GUILayout.Label(
                        $"{Trunc(r.npc, 18),-18} {r.round,5} {r.ttftMs,9:F0} {r.totalS,9:F2} {r.tokens,5} {r.TokPerSec,7:F1}",
                        Mono());
            }
            GUILayout.EndArea();
        }

        static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        static GUIStyle Rich(int size)
        {
            var st = new GUIStyle(GUI.skin.label) { richText = true, fontSize = size };
            return st;
        }
        static GUIStyle Mono()
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            st.font = Font.CreateDynamicFontFromOSFont("Consolas", 12) ?? st.font;
            return st;
        }
    }
}
