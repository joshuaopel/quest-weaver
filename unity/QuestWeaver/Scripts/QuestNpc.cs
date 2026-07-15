// Quest Weaver — the quest-giving NPC.
// Identity (name + personality) lives on the NPC; the quest (lore + rules) is
// a QuestDefinition asset. Latency design: the model is warmed at load and
// EVERY NPC prefetches its line as soon as the model is ready (prefetchOnStart),
// so by the time the player walks anywhere, words are already cached. Entering
// the trigger radius re-prefetches if the player's profile changed.

using System.Collections;
using System.Text;
using UnityEngine;

namespace QuestWeaver
{
    [RequireComponent(typeof(SphereCollider))]
    public class QuestNpc : MonoBehaviour
    {
        [Header("Who this NPC is")]
        public string npcName = "Quest Giver";
        [TextArea(2, 6)] public string personality = "Plain-spoken and busy.";

        [Header("What they hand out")]
        public QuestDefinition quest;

        [Header("Wiring")]
        public OllamaClient ollama;
        [Tooltip("Generate this NPC's line as soon as the model is warm (hot for all NPCs)")]
        public bool prefetchOnStart = true;
        [Tooltip("Also (re)generate when the player comes this close (meters)")]
        public float prefetchRadius = 10f;
        [Tooltip("E works when the player is this close (meters)")]
        public float interactRadius = 3f;

        public enum State { Idle, Generating, Ready, Failed }
        public State CurrentState { get; private set; } = State.Idle;

        // streamed text so far (UI reads this every frame)
        public string Text => textBuf.ToString();
        public bool Done { get; private set; }
        public string LastError { get; private set; }

        readonly StringBuilder textBuf = new StringBuilder();
        int cachedProfileHash;
        string cachedText;
        QuestPlayer playerInRange;
        Coroutine job;

        public string SystemPrompt() { return QuestPrompt.System(npcName, personality, quest); }

        void Reset() { GetComponent<SphereCollider>().isTrigger = true; }

        void Awake()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = prefetchRadius;
        }

        void Start()
        {
            if (prefetchOnStart) StartCoroutine(PrefetchWhenWarm());
        }

        IEnumerator PrefetchWhenWarm()
        {
            float deadline = Time.time + 30f;             // don't wait forever on a dead server
            while (ollama != null && !ollama.IsWarm && Time.time < deadline)
                yield return null;
            var qp = FindPlayer();
            if (qp != null && ollama != null) Prefetch(qp.profile);
        }

        static QuestPlayer FindPlayer()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<QuestPlayer>();
#else
            return Object.FindObjectOfType<QuestPlayer>();
#endif
        }

        void OnTriggerEnter(Collider other)
        {
            var qp = other.GetComponentInParent<QuestPlayer>();
            if (qp == null) return;
            playerInRange = qp;
            Prefetch(qp.profile);          // no-op if already cached for this profile
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<QuestPlayer>() == playerInRange)
                playerInRange = null;
        }

        public bool PlayerCanInteract()
        {
            return playerInRange != null &&
                   Vector3.Distance(playerInRange.transform.position, transform.position) <= interactRadius;
        }

        /// <summary>Begin (or reuse) generation for this profile.</summary>
        public void Prefetch(PlayerProfile profile)
        {
            int h = profile.Hash();
            if (h == cachedProfileHash && cachedText != null)
            {   // cache hit — instant
                textBuf.Length = 0; textBuf.Append(cachedText);
                Done = true; CurrentState = State.Ready;
                return;
            }
            if (CurrentState == State.Generating && h == cachedProfileHash) return; // already in flight
            if (job != null) ollama.StopCoroutine(job);  // the coroutine runs on the client, stop it there

            cachedProfileHash = h; cachedText = null;
            textBuf.Length = 0; Done = false; LastError = null;
            CurrentState = State.Generating;

            job = ollama.Chat(
                SystemPrompt(), QuestPrompt.User(profile),
                delta => textBuf.Append(delta),
                (full, err) =>
                {
                    job = null;
                    if (err != null && string.IsNullOrEmpty(Text))
                    { LastError = err; CurrentState = State.Failed; Done = true; return; }
                    cachedText = full ?? Text;
                    Done = true; CurrentState = State.Ready;
                });
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, .8f, .3f, .4f);
            Gizmos.DrawWireSphere(transform.position, prefetchRadius);
            Gizmos.color = new Color(.4f, 1f, .5f, .6f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
