// Quest Weaver — dialogue UI, built entirely from code (no prefabs/assets).
// Typewriter reveal runs at reading speed and never overtakes the token
// stream, so network jitter is invisible: the NPC just "keeps talking".

using UnityEngine;
using UnityEngine.UI;

namespace QuestWeaver
{
    public class QuestDialogueUI : MonoBehaviour
    {
        [Tooltip("Characters revealed per second (reading speed)")]
        public float charsPerSecond = 45f;

        QuestNpc activeNpc;
        float revealed;
        Canvas canvas;
        GameObject panel;
        Text nameText, bodyText, hintText, promptText;

        void Start() { BuildUi(); }

        void Update()
        {
            // interaction prompt + open
            QuestNpc near = FindNearbyNpc();
            promptText.enabled = activeNpc == null && near != null;
            if (near != null && activeNpc == null && Input.GetKeyDown(KeyCode.E))
            {
                activeNpc = near;
                revealed = 0f;
                panel.SetActive(true);
                nameText.text = near.quest != null ? near.quest.npcName : near.name;
            }

            if (activeNpc == null) return;

            // close
            if (Input.GetKeyDown(KeyCode.E) && revealed > 1f || Input.GetKeyDown(KeyCode.Escape))
            {
                panel.SetActive(false);
                activeNpc = null;
                return;
            }

            // typewriter follows the stream
            string full = activeNpc.CurrentState == QuestNpc.State.Failed
                ? "…" + (activeNpc.LastError ?? "the words won't come") +
                  "\n(is Ollama running? ollama pull " + activeNpc.ollama.model + ")"
                : activeNpc.Text;

            revealed = Mathf.Min(revealed + charsPerSecond * Time.deltaTime, full.Length);
            bodyText.text = full.Substring(0, Mathf.FloorToInt(revealed));

            bool waiting = full.Length == 0 ||
                           (!activeNpc.Done && revealed >= full.Length);
            hintText.text = waiting ? "…" : (activeNpc.Done && revealed >= full.Length ? "[E] close" : "");
        }

        QuestNpc FindNearbyNpc()
        {
#if UNITY_2023_1_OR_NEWER
            foreach (var npc in Object.FindObjectsByType<QuestNpc>(FindObjectsSortMode.None))
#else
            foreach (var npc in Object.FindObjectsOfType<QuestNpc>())
#endif
                if (npc.PlayerCanInteract()) return npc;
            return null;
        }

        // ---------- programmatic UI ----------

        void BuildUi()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            gameObject.AddComponent<GraphicRaycaster>();

            promptText = MakeText("Prompt", canvas.transform, "[E] talk", 22, TextAnchor.MiddleCenter);
            Rect(promptText.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 140), new Vector2(300, 30));
            promptText.enabled = false;

            panel = new GameObject("DialoguePanel", typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var img = panel.GetComponent<Image>();
            img.color = new Color(0.07f, 0.05f, 0.10f, 0.92f);
            Rect(panel.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 130), new Vector2(900, 230));

            nameText = MakeText("Name", panel.transform, "", 22, TextAnchor.UpperLeft);
            nameText.color = new Color(0.83f, 0.66f, 0.31f);
            Rect(nameText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -26), new Vector2(-40, 30));

            bodyText = MakeText("Body", panel.transform, "", 20, TextAnchor.UpperLeft);
            Rect(bodyText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, -14), new Vector2(-40, -66));

            hintText = MakeText("Hint", panel.transform, "", 16, TextAnchor.LowerRight);
            hintText.color = new Color(1, 1, 1, 0.45f);
            Rect(hintText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 12), new Vector2(200, 24));

            panel.SetActive(false);
        }

        static Text MakeText(string name, Transform parent, string content, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = BuiltinFont();
            t.text = content; t.fontSize = size; t.alignment = anchor;
            t.color = new Color(0.92f, 0.89f, 0.95f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        static Font BuiltinFont()
        {
            // 2022+ ships LegacyRuntime.ttf; older editors ship Arial.ttf
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        static void Rect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            rt.pivot = new Vector2(.5f, aMin.y == 1 ? 1 : aMin.y);
        }
    }
}
