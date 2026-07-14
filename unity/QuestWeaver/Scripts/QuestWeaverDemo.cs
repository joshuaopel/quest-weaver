// Quest Weaver — one-component demo scene.
// Drop this on an empty GameObject in an empty scene and press Play:
// ground, light, capsule player (WASD), capsule NPC (E to talk), dialogue UI.
// The Ollama model is warmed on load, and the NPC prefetches dialogue the
// moment you walk near it — by the time you press E, words are waiting.

using UnityEngine;

namespace QuestWeaver
{
    public class QuestWeaverDemo : MonoBehaviour
    {
        [Tooltip("Model tag — qwen2.5:0.5b (fastest) or gemma4:e4b (best prose)")]
        public string model = "qwen2.5:0.5b";
        public string ollamaUrl = "http://localhost:11434";

        [Tooltip("Optional: assign a QuestDefinition asset; otherwise the built-in Sunken Bell example is used")]
        public QuestDefinition quest;

        public PlayerProfile playerProfile = new PlayerProfile
        {
            playerName = "Grix", gender = "male", race = "goblin",
            characterClass = "rogue", level = 19,
            reputationCrown = -55, reputationTemple = -10, reputationThieves = 85,
            deeds = "betrayed a merchant caravan; secretly paid for an orphanage roof"
        };

        void Start()
        {
            if (quest == null) quest = ScriptableObject.CreateInstance<QuestDefinition>();

            // --- LLM client (warm the model immediately: kills cold-start) ---
            var ollama = gameObject.AddComponent<OllamaClient>();
            ollama.baseUrl = ollamaUrl;
            ollama.model = model;
            ollama.Warmup();

            // --- environment ---
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6, 1, 6);
            ground.GetComponent<Renderer>().material.color = new Color(0.16f, 0.20f, 0.16f);

            var lightGo = new GameObject("Sun");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -30f, 0);

            // --- player (capsule, WASD) ---
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0, 1.1f, -12f);
            player.GetComponent<Renderer>().material.color = new Color(0.35f, 0.5f, 0.9f);
            Object.Destroy(player.GetComponent<CapsuleCollider>());   // CharacterController replaces it
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.center = Vector3.zero;
            var pc = player.AddComponent<PlayerController>();
            var qp = player.AddComponent<QuestPlayer>();
            qp.profile = playerProfile;

            if (Camera.main == null) new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).tag = "MainCamera";
            pc.cam = Camera.main.transform;

            // --- NPC (capsule, prefetch + E to talk) ---
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC " + quest.npcName;
            npc.transform.position = new Vector3(0, 1.1f, 4f);
            npc.GetComponent<Renderer>().material.color = new Color(0.85f, 0.68f, 0.3f);
            Object.Destroy(npc.GetComponent<CapsuleCollider>());
            var questNpc = npc.AddComponent<QuestNpc>();          // adds its own trigger sphere
            questNpc.quest = quest;
            questNpc.ollama = ollama;

            // simple "!" marker over the NPC's head
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "QuestMarker";
            marker.transform.SetParent(npc.transform, false);
            marker.transform.localPosition = new Vector3(0, 1.6f, 0);
            marker.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            marker.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.2f);
            Object.Destroy(marker.GetComponent<BoxCollider>());

            // --- dialogue UI ---
            new GameObject("QuestDialogueUI").AddComponent<QuestDialogueUI>();
        }
    }
}
