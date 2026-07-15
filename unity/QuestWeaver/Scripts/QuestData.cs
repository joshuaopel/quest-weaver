// Quest Weaver — data types + prompt builder.
// Prompt layout is deliberately KV-cache-friendly: everything static (NPC
// persona, quest lore, rules) comes first and is byte-identical between
// requests; the player block comes last, so Ollama only re-evaluates the
// small changing tail.

using System;
using System.Text;
using UnityEngine;

namespace QuestWeaver
{
    /// <summary>The curated quest: lore + the rules the prompt must stick to.</summary>
    [CreateAssetMenu(menuName = "Quest Weaver/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        public string questTitle = "The Sunken Bell";

        [TextArea(4, 12), Tooltip("The quest lore — story, stakes, task, reward")]
        public string lore =
            "Grayharbor's great bronze bell — blessed to keep the fog-wraiths at bay — was stolen a week ago and sunk in the flooded crypts beneath the old lighthouse. Each night the fog creeps further up the streets, and each morning another fisher does not wake. The player must recover the bell from the flooded crypt and return it before nightfall. Reward: 60 silver and the town's gratitude.";

        [TextArea(3, 10), Tooltip("Hard rules the generated dialogue must obey — canon, secrets, limits")]
        public string rules =
            "Never change the task, the location, or the reward (exactly 60 silver). Never reveal who stole the bell. Always warn that the crypt floods waist-deep and something moves in the water. The quest must sound completable by any class.";
    }

    /// <summary>The player's narrative identity — all inspector-editable.</summary>
    [Serializable]
    public class PlayerProfile
    {
        public string playerName = "Adventurer";
        public string gender = "female";
        public string race = "human";
        public string characterClass = "paladin";
        public int level = 20;

        [TextArea(2, 4), Tooltip("Who this character is, in a sentence or two")]
        public string bio = "";
        [TextArea(2, 4), Tooltip("Notable deeds — what they've done")]
        public string deeds = "";
        [TextArea(2, 4), Tooltip("Feats — titles, achievements, famous victories")]
        public string feats = "";
        [TextArea(2, 4), Tooltip("Affiliations & reputation, e.g. 'Thieves' Den: revered; The Crown: hated'")]
        public string affiliations = "";

        /// <summary>Stable hash — used as the dialogue cache key.</summary>
        public int Hash()
        {
            unchecked
            {
                int h = 17;
                foreach (string s in new[] { playerName, gender, race, characterClass,
                                             bio, deeds, feats, affiliations })
                    h = h * 31 + (s ?? "").GetHashCode();
                return h * 31 + level;
            }
        }
    }

    public static class QuestPrompt
    {
        /// <summary>Static block — identical for every player, so Ollama's prompt
        /// prefix cache makes repeat generations nearly free to evaluate.</summary>
        public static string System(string npcName, string npcPersonality, QuestDefinition q)
        {
            return
"You are " + npcName + ", an NPC quest giver in a fantasy game. A player approaches; greet them and give them the quest.\n\n" +
"YOUR PERSONALITY (stay in this voice, always): " + npcPersonality + "\n\n" +
"QUEST — " + q.questTitle + ":\n" + q.lore + "\n\n" +
"RULES YOU MUST STICK TO:\n" + q.rules + "\n\n" +
"OUTPUT RULES:\n" +
"- Spoken dialogue ONLY: no stage directions, no lists, no headings, no notes, no meta-commentary.\n" +
"- Start with a greeting that uses the player's name, then deliver the quest details.\n" +
"- Two short paragraphs, 90 words maximum total.\n" +
"- Personalize tone and motivation using the player's bio, deeds, feats and affiliations — but never break the quest rules or your personality.";
        }

        /// <summary>Small changing tail: just the player.</summary>
        public static string User(PlayerProfile p)
        {
            var sb = new StringBuilder(384);
            sb.Append("PLAYER: ").Append(p.playerName)
              .Append(", level ").Append(p.level)
              .Append(' ').Append(p.gender)
              .Append(' ').Append(p.race)
              .Append(' ').Append(p.characterClass).Append('.');
            if (!string.IsNullOrEmpty(p.bio))          sb.Append("\nBIO: ").Append(p.bio);
            if (!string.IsNullOrEmpty(p.deeds))        sb.Append("\nDEEDS: ").Append(p.deeds);
            if (!string.IsNullOrEmpty(p.feats))        sb.Append("\nFEATS: ").Append(p.feats);
            if (!string.IsNullOrEmpty(p.affiliations)) sb.Append("\nAFFILIATIONS: ").Append(p.affiliations);
            sb.Append("\n\nThe player walks up to you now. Greet them and give the quest.");
            return sb.ToString();
        }
    }
}
