// Quest Weaver — one-component demo scene.
// Drop this on an empty GameObject in an empty scene and press Play:
// ground, light, capsule player (WASD), talkable capsule NPCs (E), dialogue UI,
// and an LLM stress test on F5. The model is warmed at load and EVERY NPC
// prefetches its line immediately — hot and ready before the player moves.

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

        [Header("Player — narrative identity (edit freely)")]
        public PlayerProfile playerProfile = new PlayerProfile
        {
            playerName = "Grix",
            gender = "male",
            race = "goblin",
            characterClass = "rogue",
            level = 19,
            bio = "A wiry goblin cutpurse gone semi-respectable, always counting exits.",
            deeds = "Betrayed a merchant caravan to the Den; secretly paid for an orphanage roof in Grayharbor.",
            feats = "Escaped the Crown's gibbets twice; picked the Unpickable Lock of Veldt.",
            affiliations = "Thieves' Den: revered; The Crown: hated; Merchant Guild: wary of him."
        };

        /// <summary>One spawnable NPC — identity + quest, authorable in the inspector.</summary>
        [Serializable]
        public class NpcSpawn
        {
            public string npcName = "NPC";
            [TextArea(2, 6)] public string personality = "";
            public string questTitle = "A Task";
            [TextArea(3, 8), Tooltip("Quest lore — story, stakes, task, reward")]
            public string lore = "";
            [TextArea(2, 6), Tooltip("Hard rules the dialogue must stick to")]
            public string rules = "";
            public Vector3 position = new Vector3(0, 1.1f, 4f);
            public Color color = new Color(0.85f, 0.68f, 0.3f);
        }

        [Header("NPCs — every entry becomes a talkable quest giver")]
        public List<NpcSpawn> npcs = new List<NpcSpawn>
        {
            new NpcSpawn {
                npcName = "Maren Tolch",
                personality = "Weathered innkeeper, late fifties. Brisk and practical; grief hidden under a dry wit. Never begs — she recruits. Won't name the fog-wraiths aloud after dark.",
                questTitle = "The Sunken Bell",
                lore = "Grayharbor's great bronze bell — blessed to keep the fog-wraiths at bay — was stolen a week ago and sunk in the flooded crypts beneath the old lighthouse. Each night the fog creeps further up the streets, and each morning another fisher does not wake. Recover the bell and return it before nightfall. Reward: 60 silver and the town's gratitude.",
                rules = "Never change the task, location, or reward (exactly 60 silver). Never reveal who stole the bell. Always warn that the crypt floods waist-deep and something moves in the water.",
                position = new Vector3(0, 1.1f, 5f), color = new Color(0.85f, 0.68f, 0.3f)
            },
            new NpcSpawn {
                npcName = "Torvald Emberhand",
                personality = "Gruff old blacksmith. Short sentences. Hides kindness behind complaints. Hates being thanked, hates idle hands more.",
                questTitle = "Iron for the Watch",
                lore = "Bandits hit the iron wagon at Redford Pass three days back — every sword blank meant for the town watch, gone. The winter festival is coming and the roads crawl with wolves and worse. Track the wagon into Redford Pass and bring back the blanks before the festival. Reward: 40 silver and a weapon reforged free of charge.",
                rules = "The reward is exactly 40 silver plus one free reforging — never more. Never speculate about who the bandits are. Always mention the festival deadline.",
                position = new Vector3(12, 1.1f, 11f), color = new Color(0.55f, 0.32f, 0.22f)
            },
            new NpcSpawn {
                npcName = "Whisper",
                personality = "Dockside fence and information broker. Never says anything directly; speaks in trade metaphors and half-finished sentences. Purring, unhurried, always collecting favors.",
                questTitle = "The Harbormaster's Ledger",
                lore = "The harbormaster keeps a private ledger of every bribe that ever crossed the docks — half the town is in it, and yesterday he started selling pages. Slip into his locked office and bring the ledger back, unopened and unread. Reward: 80 silver, no questions, and a favor owed.",
                rules = "Never name anyone who appears in the ledger. Never say the word 'steal' — Whisper calls it 'repatriation'. The ledger must come back unopened; make that clear.",
                position = new Vector3(-12, 1.1f, 9f), color = new Color(0.4f, 0.32f, 0.55f)
            }
        };

        [Header("Stress test")]
        [Tooltip("Rounds of the full NPC roster the F5 stress test runs")]
        public int stressTestRounds = 3;

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
            player.name = "Player " + playerProfile.playerName;
            player.transform.position = new Vector3(0, 1.1f, -12f);
            player.GetComponent<Renderer>().material.color = new Color(0.35f, 0.5f, 0.9f);
            UnityEngine.Object.Destroy(player.GetComponent<CapsuleCollider>());   // CharacterController replaces it
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.center = Vector3.zero;
            var pc = player.AddComponent<PlayerController>();
            var qp = player.AddComponent<QuestPlayer>();
            qp.profile = playerProfile;

            if (Camera.main == null) new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).tag = "MainCamera";
            pc.cam = Camera.main.transform;

            // --- NPCs: all hot — each prefetches as soon as the model is warm ---
            foreach (var spawn in npcs)
                SpawnNpc(spawn, ollama);

            // --- dialogue UI + stress test ---
            new GameObject("QuestDialogueUI").AddComponent<QuestDialogueUI>();
            var stress = gameObject.AddComponent<QuestStressTest>();
            stress.ollama = ollama;
            stress.rounds = stressTestRounds;
        }

        static void SpawnNpc(NpcSpawn spawn, OllamaClient ollama)
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.questTitle = spawn.questTitle;
            quest.lore = spawn.lore;
            quest.rules = spawn.rules;

            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC " + spawn.npcName;
            npc.transform.position = spawn.position;
            npc.GetComponent<Renderer>().material.color = spawn.color;
            UnityEngine.Object.Destroy(npc.GetComponent<CapsuleCollider>());
            var questNpc = npc.AddComponent<QuestNpc>();          // adds its own trigger sphere
            questNpc.npcName = spawn.npcName;
            questNpc.personality = spawn.personality;
            questNpc.quest = quest;
            questNpc.ollama = ollama;
            questNpc.prefetchOnStart = true;

            // "!" marker over the head
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "QuestMarker";
            marker.transform.SetParent(npc.transform, false);
            marker.transform.localPosition = new Vector3(0, 1.6f, 0);
            marker.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            marker.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.2f);
            UnityEngine.Object.Destroy(marker.GetComponent<BoxCollider>());

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
