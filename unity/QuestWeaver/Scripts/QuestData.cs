// Quest Weaver — data types + prompt builder.
// Prompt layout is deliberately KV-cache-friendly: everything static (rules,
// quest, persona) comes first and is byte-identical between requests; the
// player block comes last, so Ollama only re-evaluates the small changing tail.

using System;
using System.Text;
using UnityEngine;

namespace QuestWeaver
{
    [CreateAssetMenu(menuName = "Quest Weaver/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        public string questTitle = "The Sunken Bell";
        [TextArea(4, 10)] public string flavorText =
            "The fishing town of Grayharbor has gone quiet. Its great bronze bell — blessed to keep the fog-wraiths at bay — was stolen a week ago and dropped into the flooded crypts beneath the old lighthouse. Each night the fog creeps further up the streets, and each morning another fisher does not wake.";
        public string npcName = "Maren Tolch";
        [TextArea(3, 8)] public string npcPersonality =
            "Weathered innkeeper, late fifties. Brisk and practical; grief hidden under a dry wit. Fiercely protective of Grayharbor. Never begs — she recruits. Won't name the fog-wraiths aloud after dark.";
        [TextArea(3, 8)] public string task =
            "The player must recover the Bronze Bell of Grayharbor from the flooded crypt beneath the old lighthouse and return it before nightfall. Reward: 60 silver and the town's gratitude.";
    }

    [Serializable]
    public class PlayerProfile
    {
        public string playerName = "Adventurer";
        public string gender = "female";
        public string race = "human";
        public string characterClass = "paladin";
        public int level = 20;
        [Range(-100, 100)] public int reputationCrown = 0;
        [Range(-100, 100)] public int reputationTemple = 0;
        [Range(-100, 100)] public int reputationThieves = 0;
        [TextArea(2, 4)] public string deeds = "";

        public static string Tier(int v)
        {
            if (v >= 75) return "revered";  if (v >= 40) return "trusted";
            if (v >= 15) return "friendly"; if (v > -15) return "neutral";
            if (v > -40) return "wary";     if (v > -75) return "disliked";
            return "hated";
        }

        /// <summary>Stable hash — used as the dialogue cache key.</summary>
        public int Hash()
        {
            unchecked
            {
                int h = 17;
                foreach (string s in new[] { playerName, gender, race, characterClass, deeds })
                    h = h * 31 + (s ?? "").GetHashCode();
                h = h * 31 + level;
                h = h * 31 + reputationCrown;
                h = h * 31 + reputationTemple;
                h = h * 31 + reputationThieves;
                return h;
            }
        }
    }

    public static class QuestPrompt
    {
        // Static, identical for every player -> maximizes Ollama's prompt prefix cache.
        public static string System(QuestDefinition q)
        {
            return
"You are the voice of one NPC quest giver in a fantasy game. You receive a curated quest and a player profile, and you speak the quest to that player.\n" +
"Rules:\n" +
"- Output ONLY the NPC's spoken dialogue. No stage directions, no lists, no headings, no notes, no quotation marks around the whole thing.\n" +
"- Two short paragraphs, 90 words maximum total. Every word is speech.\n" +
"- Stay strictly in the NPC's authored personality. Reputation changes their attitude toward this player, never who they are.\n" +
"- Never change the task, the location, or the reward. Personalize the greeting, tone, motivation, and how the task is pitched (class, race, gender, reputation, deeds).\n\n" +
"QUEST: " + q.questTitle + "\n" +
"STORY: " + q.flavorText + "\n" +
"NPC: " + q.npcName + "\n" +
"NPC PERSONALITY: " + q.npcPersonality + "\n" +
"TASK (fixed): " + q.task;
        }

        // Small changing tail: just the player.
        public static string User(PlayerProfile p)
        {
            var sb = new StringBuilder(256);
            sb.Append("PLAYER: ").Append(p.playerName)
              .Append(", level ").Append(p.level)
              .Append(' ').Append(p.gender)
              .Append(' ').Append(p.race)
              .Append(' ').Append(p.characterClass).Append(".\n");
            sb.Append("Reputation — Crown: ").Append(PlayerProfile.Tier(p.reputationCrown))
              .Append(", Temple: ").Append(PlayerProfile.Tier(p.reputationTemple))
              .Append(", Thieves' Den: ").Append(PlayerProfile.Tier(p.reputationThieves)).Append(".\n");
            if (!string.IsNullOrEmpty(p.deeds)) sb.Append("Known deeds: ").Append(p.deeds).Append('\n');
            sb.Append("Speak the quest to this player now.");
            return sb.ToString();
        }
    }
}
