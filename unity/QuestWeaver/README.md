# Quest Weaver — Unity plugin

Walk a capsule up to an NPC, press **E**, and a local LLM (Gemma 4 / Qwen 2.5
via Ollama) speaks the quest to *your* character — personalized by race, class,
gender, reputation, and deeds. Dialogue only: no notes, no lists, just what the
NPC says.

## Install (2 minutes)

1. Copy the `QuestWeaver` folder into your project's `Assets/`.
   Requires Unity **2021.3+** (built-in render pipeline or URP; uses only
   UnityEngine + UnityWebRequest + legacy UI — no packages).
2. Make sure Ollama is running and a model is pulled:
   ```
   ollama pull qwen2.5:0.5b     (fastest, ~0.4 GB)
   ollama pull gemma4:e4b       (best prose, ~9.6 GB)
   ```
3. **Demo scene:** create an empty scene, add an empty GameObject, add the
   `QuestWeaverDemo` component, press **Play**. You get a ground plane, a blue
   capsule (you — WASD to move) and **three quest-giver capsules** with name
   tags and "!" markers. Walk up to any of them, press **E**, read.

Pick the model on the `QuestWeaverDemo` inspector (`qwen2.5:0.5b` default —
snappiest for a demo). Edit the **Player Profile** fields in the inspector and
re-enter Play mode to hear the same quest told differently.

## Multiple NPCs

The demo spawns **three NPCs out of the box**, each with their own story,
personality, and quest: Maren Tolch (innkeeper, *The Sunken Bell*), Torvald
Emberhand (blacksmith, *Iron for the Watch*), and Whisper (dockside fence,
*The Harbormaster's Ledger*). The `Npcs` list on `QuestWeaverDemo` is plain
inspector data — edit the entries or press **+** to add a fourth, fifth,
twentieth: name, personality, story, task, position, capsule color. Each
becomes a talkable capsule with a name tag, its own prefetch trigger, and its
own dialogue cache. Walking between them shows the same player greeted three
completely different ways.

Note on concurrency: if several NPCs' prefetch radii overlap, Ollama queues the
generations (typically fine — the second one still finishes long before the
player finds their E key). Space quest givers ~15 m apart or shrink
`prefetchRadius` if you pack them tighter.

## Using it in your own scene

- Put `OllamaClient` on any persistent GameObject; call `Warmup()` at load.
- Put `QuestNpc` on your NPC (it adds its own trigger sphere), assign a
  `QuestDefinition` asset (right-click → Create → Quest Weaver → Quest
  Definition) and the `OllamaClient`.
- Add `QuestPlayer` to your player object and fill its `PlayerProfile`.
- Add `QuestDialogueUI` to an empty GameObject (it builds its own canvas), or
  read `QuestNpc.Text` / `.Done` yourself and render however you like.

## How response time is minimized

The demo's goal is that the player never watches a spinner:

| Trick | What it buys |
|---|---|
| **Proximity prefetch** — `QuestNpc` starts generating when the player enters a ~10 m trigger, not when E is pressed | The 2–3 s of walk-up time absorbs first-token latency; text is usually waiting before the box opens |
| **Warm-up on load** — 1-token request with `keep_alive: 30m` at scene start | Removes the 10–30 s cold model load from the first interaction |
| **Streaming + typewriter** — tokens append to `QuestNpc.Text`; the UI reveals ~45 chars/s and never overtakes the stream | Reading speed < generation speed, so the NPC just "talks"; jitter is invisible |
| **Dialogue-only, hard-capped** — plain text (no JSON schema), ≤ 90 words, `num_predict` 150 | First visible word arrives immediately; total generation stays ~3–8 s even on small models |
| **KV-cache-friendly prompts** — static system block (rules + quest + persona) first, tiny player block last | Ollama reuses the cached prefix; repeat generations skip most prompt evaluation |
| **Profile-hash cache** — finished dialogue is cached per (NPC, profile) | Talking again is instant; regenerates only when the profile changes |

Rule of thumb on a mid-range GPU: `qwen2.5:0.5b` streams faster than anyone
reads and finishes a quest line in ~1–3 s; `gemma4:e4b` writes better and still
feels instant *because of the prefetch* — the generation happened while the
player was walking over.

## Files

```
Scripts/OllamaClient.cs      streaming NDJSON client + warmup (UnityWebRequest)
Scripts/QuestData.cs         QuestDefinition asset, PlayerProfile, prompt builder
Scripts/QuestNpc.cs          prefetch trigger, state machine, dialogue cache
Scripts/QuestDialogueUI.cs   code-built canvas: prompt, panel, typewriter
Scripts/PlayerController.cs  capsule WASD + follow camera; QuestPlayer profile holder
Scripts/QuestWeaverDemo.cs   one-component bootstrap demo scene
```

*Status: written against standard Unity 2021.3+ APIs but authored outside the
editor — if your Unity version flags anything, it will be small (font name or
FindObjectsOfType deprecation warnings).*
