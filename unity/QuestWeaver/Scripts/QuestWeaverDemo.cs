// Quest Weaver — one-component demo scene.
// Drop this on an empty GameObject in an empty scene and press Play:
// ground, light, capsule player (WASD), capsule NPC (E to talk), dialogue UI.
// The Ollama model is warmed on load, and the NPC prefetches dialogue the
// moment you walk near it — by the time you press E, words are waiting.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuestWeaver
{
    public class QuestWeaverDemo : MonoBehaviour
    {
        [Tooltip("Model tag — qwen2.5:0.5b (fastest) or gemma4:e4b (best prose)")]
        public string model = "qwen2.5:0.5b";
        public string ollamaUrl = "http://localhost:11434";

        /// <summary>One spawnable NPC, authorable straight in the inspector.</summary>
        [Serializable]
        public class NpcSpawn
        {
            public string npcName = "NPC";
            public string questTitle = "A Task";
            [TextArea(2, 6)] public string personality = "";
            [TextArea(3, 8)] public string story = "";
            [TextArea(2, 6)] public string task = "";
            public Vector3 position = new Vector3(0, 1.1f, 4f);
            public Color color = new Color(0.85f, 0.68f, 0.3f);
        }

        [Tooltip("Every entry becomes a talkable capsule NPC with its own quest. Add as many as you like.")]
        public List<NpcSpawn> npcs = new List<NpcSpawn>
        {
            new NpcSpawn {
                npcName = "Maren Tolch", questTitle = "The Sunken Bell",
                personality = "Weathered innkeeper, late fifties. Brisk and practical; grief hidden under a dry wit. Fiercely protective of Grayharbor. Never begs — she recruits. Won't name the fog-wraiths aloud after dark.",
                story = "Grayharbor's great bronze bell — blessed to keep the fog-wraiths at bay — was stolen a week ago and sunk in the flooded crypts beneath the old lighthouse. Each night the fog creeps further up the streets, and each morning another fisher does not wake.",
                task = "Recover the Bronze Bell of Grayharbor from the flooded crypt beneath the old lighthouse and return it before nightfall. Reward: 60 silver and the town's gratitude.",
                position = new Vector3(0, 1.1f, 5f), color = new Color(0.85f, 0.68f, 0.3f)
            },
            new NpcSpawn {
                npcName = "Torvald Emberhand", questTitle = "Iron for the Watch",
                personality = "Gruff old blacksmith. Short sentences. Hides kindness behind complaints. Hates being thanked, hates idle hands more. Proud of every blade he has ever hammered.",
                story = "Bandits hit the iron wagon at Redford Pass three days back — every sword blank meant for the town watch, gone. The winter festival is coming, the roads crawl with wolves and worse, and Torvald will not send the watch out with kitchen knives.",
                task = "Track the stolen iron wagon into Redford Pass and bring back the sword blanks before the festival. Reward: 40 silver and a weapon reforged free of charge.",
                position = new Vector3(12, 1.1f, 11f), color = new Color(0.55f, 0.32f, 0.22f)
            },
            new NpcSpawn {
                npcName = "Whisper", questTitle = "The Harbormaster's Ledger",
                personality = "Dockside fence and information broker. Never says anything directly; speaks in trade metaphors and half-finished sentences. Purring, unhurried, always collecting favors. Genuinely dangerous, never rude.",
                story = "The harbormaster keeps a private ledger of every bribe that ever crossed the docks — half the town is in it, and yesterday he started selling pages. People whose names are inked in that book are getting nervous, and nervous people pay well.",
                task = "Slip into the harbormaster's locked office and bring the ledger back, unopened and unread. Reward: 80 silver, no questions, and a favor owed by someone who never forgets.",
                position = new Vector3(-12, 1.1f, 9f), color = new Color(0.4f, 0.32f, 0.55f)
            }
        };

        public PlayerProfile playerProfile = new PlayerProfile
        {
            playerName = "Grix", gender = "male", race = "goblin",
            characterClass = "rogue", level = 19,
            reputationCrown = -55, reputationTemple = -10, reputationThieves = 85,
            deeds = "betrayed a merchant caravan; secretly paid for an orphanage roof"
        };

        void Start()
        {
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

            // --- NPCs (capsules, prefetch + E to talk), one per list entry ---
            foreach (var spawn in npcs)
                SpawnNpc(spawn, ollama);

            // --- dialogue UI ---
            new GameObject("QuestDialogueUI").AddComponent<QuestDialogueUI>();
        }

        static void SpawnNpc(NpcSpawn spawn, OllamaClient ollama)
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.questTitle = spawn.questTitle;
            quest.flavorText = spawn.story;
            quest.npcName = spawn.npcName;
            quest.npcPersonality = spawn.personality;
            quest.task = spawn.task;

            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC " + spawn.npcName;
            npc.transform.position = spawn.position;
            npc.GetComponent<Renderer>().material.color = spawn.color;
            Object.Destroy(npc.GetComponent<CapsuleCollider>());
            var questNpc = npc.AddComponent<QuestNpc>();          // adds its own trigger sphere
            questNpc.quest = quest;
            questNpc.ollama = ollama;

            // "!" marker over the head
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "QuestMarker";
            marker.transform.SetParent(npc.transform, false);
            marker.transform.localPosition = new Vector3(0, 1.6f, 0);
            marker.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            marker.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.2f);
            Object.Destroy(marker.GetComponent<BoxCollider>());

            // floating name tag that faces the player
            var tagGo = new GameObject("NameTag");
            tagGo.transform.SetParent(npc.transform, false);
            tagGo.transform.localPosition = new Vector3(0, 2.4f, 0);
            var tm = tagGo.AddComponent<TextMesh>();
            tm.text = spawn.npcName;
            tm.fontSize = 48; tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.92f, 0.75f);
            tagGo.AddComponent<FaceCamera>();
        }
    }

    /// <summary>Billboards a transform toward the main camera (for name tags).</summary>
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
