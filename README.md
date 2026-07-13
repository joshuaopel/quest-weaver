# ⚔ Quest Weaver — dynamic quest AI demo

**One curated quest. A unique telling for every player.**

Quest Weaver is a local, single-file webtool that demonstrates how AI can *assist*
curated storytelling in an MMO — not replace it. A designer authors a quest once
(flavor text + hard rules), and a **local LLM** retells it for each individual
player, adapting to their **reputation, gender, class, race, level, and past
deeds** — while the objectives, canon, and reward budget stay locked.

Nothing leaves your machine: the model runs locally via [Ollama](https://ollama.com).

## The core idea

| Designers own (fixed) | AI adapts (per player) |
|---|---|
| Objectives & their order | NPC attitude & greeting (reputation-driven) |
| Locations & named NPCs | Motivation & how tasks are pitched (class/race) |
| Quest giver's core personality | The quest giver's *attitude* toward this player |
| Canon facts & twists | Gendered address & pronouns |
| Reward *budget* | Reward *phrasing* within budget |
| Completability rules | Tone, callbacks to the player's history |

The tool shows this contract explicitly: every generated quest card ends with
**"Why it changed for this player"** — the profile factors the model used — plus a
short note to the designer.

## Quickstart

1. **Install Ollama** — <https://ollama.com/download>
2. **Pull a lightweight model** (Gemma 4 E4B is the sweet spot for this demo —
   ~4.5B effective params, built for on-device use):
   ```bash
   ollama pull gemma4:e4b
   ```
   Prefer something tiny? **Qwen 2.5 0.5B** runs in under 1 GB of VRAM:
   ```bash
   ollama pull qwen2.5:0.5b
   ```
   The header selector offers a curated **Gemma vs Qwen** roster (plus anything
   else you've pulled); models you don't have yet show a ⬇ mark and a one-click
   **pull** button that downloads them with live progress. Each family
   automatically gets its vendor-recommended sampling (Gemma: temp 1.0 /
   top-p 0.95; Qwen: temp 0.7 / top-p 0.8).
3. **Serve the tool** (any static server; needed so the browser can call Ollama):
   ```bash
   python3 -m http.server 8080
   # then open http://localhost:8080
   ```
   Opening `index.html` directly by double-click usually works too — Ollama
   allows `file://` origins by default.

No build step, no npm, no dependencies — `index.html` is the whole app.

### No Ollama? Mock mode

If Ollama isn't running (or you tick **mock mode**), a deterministic in-page
template generator takes over so the UI and the *concept* still demo — clearly
tagged `MOCK` on each card. It personalizes off the same profile factors, just
without generative variety.

## Using the demo

1. Left panel — the **curated quest**: flavor text, the quest giver's name and
   **authored personality** (the voice the AI must stay in), rules, and reward
   budget. A complete example ("The Sunken Bell") ships in; edit anything or
   write your own.
2. Middle panel — the **player profile**. Five presets included, from a revered
   human paladin to a feared undead necromancer. Reputation sliders cover five
   factions from −100 (hated) to +100 (revered); **clickable history/trait tags**
   ("dragonslayer", "ex-convict", "cursed", …) toggle facts the model weaves in.
3. **✦ Weave Quest** generates the personalized quest. Cards stack in the right
   panel so you can compare tellings side by side.
4. **Weave for all presets** runs the same quest through every preset in one go —
   the best way to show a stakeholder how the innkeeper's tone shifts from
   *"By the Flame, I hardly dared hope!"* to bargaining through a bolted door.

Open **"Prompt preview"** in the middle panel to see exactly what is sent to the
model — useful for explaining the technique.

## How it works

- The browser calls Ollama's `/api/chat` directly (`http://localhost:11434`,
  configurable via ⚙).
- A **system prompt** encodes the designer/AI contract (what's fixed vs. adaptable);
  the **user prompt** carries the curated quest + the full player profile with
  reputation tiers (revered / trusted / friendly / neutral / wary / disliked / hated).
- Generation uses Ollama's **structured outputs** (`format` = JSON schema), so the
  model must return `quest_title`, `npc_greeting`, `objectives`,
  `personalized_hooks`, `reward_offer`, `designer_notes`, etc. — which the tool
  renders as a quest card. Tokens stream live to a console while generating.

## Troubleshooting

- **"Ollama offline"** — start it: `ollama serve` (or launch the desktop app).
- **CORS errors** in the browser console — allow your origin:
  ```bash
  OLLAMA_ORIGINS="*" ollama serve
  ```
- **Slow generation** — a small model on CPU takes a while for its first response
  (model load). Subsequent generations are much faster; or use `gemma4:e2b`.
- **Malformed output** — raise/lower temperature in ⚙ settings, or try a larger
  model. The JSON schema constraint makes this rare.

## Why this matters for game design

Hand-written quests are static: every paladin and every necromancer reads the
same three paragraphs. Fully generated quests are slop: no canon, no arcs, no
authorial intent. Quest Weaver demos the middle path — **designers curate the
what, AI performs the telling** — cheap enough to run per-player on local
hardware, and constrained enough to ship.
