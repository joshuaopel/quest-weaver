// Quest Weaver — the quest-giving NPC.
// Latency design: generation starts when the player enters the prefetch radius
// (walk-up time hides first-token latency); pressing E opens the dialogue UI,
// which follows the stream with a typewriter. Finished lines are cached per
// player-profile hash, so talking again is instant until the profile changes.

using System.Text;
using UnityEngine;

namespace QuestWeaver
{
    [RequireComponent(typeof(SphereCollider))]
    public class QuestNpc : MonoBehaviour
    {
        public QuestDefinition quest;
        public OllamaClient ollama;

        [Tooltip("Start generating when the player is this close (meters)")]
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

        void Reset() { GetComponent<SphereCollider>().isTrigger = true; }

        void Awake()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = prefetchRadius;
        }

        void OnTriggerEnter(Collider other)
        {
            var qp = other.GetComponentInParent<QuestPlayer>();
            if (qp == null) return;
            playerInRange = qp;
            Prefetch(qp.profile);              // the whole trick: start early
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
                QuestPrompt.System(quest), QuestPrompt.User(profile),
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
