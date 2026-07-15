# 🧪 Tester setup guide — get the local AI running

Welcome! Quest Weaver demos AI-personalized quest dialogue running **entirely on
your own computer** — nothing goes to the cloud, no account, no API key. The
only setup that takes real steps is installing the local AI. This guide holds
your hand through it.

**What you need:** Windows 10/11 (Mac/Linux notes at the bottom), ~8 GB RAM,
and 1–10 GB free disk depending on which model you pick.

---

## The fast path (Windows, ~3 minutes)

1. **Download this repo** — green **Code** button → *Download ZIP* → unzip
   anywhere (or `git clone` it if you're a git person).
2. **Double-click `setup.bat`** in the unzipped folder.
   It will:
   - install **Ollama** (the free app that runs AI models locally) if you
     don't have it,
   - ask which model you want — **Gemma 4 e4b** (best writing, 9.6 GB) is the
     default; pick the smaller options only if your PC or disk is tight,
   - open Quest Weaver in your browser.
3. When the badge in the top-right corner turns **green ("Ollama connected")**:
   pick a player preset, click **✦ Weave Quest**, and watch the innkeeper greet
   your character personally.

If anything goes wrong, the [Troubleshooting](#troubleshooting) section below
covers every failure testers have hit so far.

---

## The manual path (what setup.bat does, step by step)

### Step 1 — Install Ollama

Ollama is a free, open-source app that downloads and runs AI models on your
machine. It's the only install.

- Go to **<https://ollama.com/download>** and run the installer
  (no admin rights needed — it installs to your user folder), **or** in
  PowerShell:
  ```powershell
  irm https://ollama.com/install.ps1 | iex
  ```
- After install you'll see a **llama icon in your system tray** (bottom-right,
  near the clock). That means the AI server is running. It starts automatically
  with Windows from now on.

### Step 2 — Download a model

Open Command Prompt (Win+R → `cmd` → Enter) and run **one** of these:

| Command | Size | Needs | Quality |
|---|---|---|---|
| `ollama pull gemma4:e4b` | 9.6 GB | ~10 GB VRAM/RAM | **best — pick this if you can** |
| `ollama pull gemma4:e2b` | 7.2 GB | ~6 GB VRAM/RAM | good |
| `ollama pull qwen2.5:3b` | 1.9 GB | any modern PC | decent, light |
| `ollama pull qwen2.5:0.5b` | 0.4 GB | anything | weak prose — pipeline testing only |

You'll see a progress bar; when it says `success` you're done. (You can also
skip this step — Quest Weaver has a **⬇ pull** button in its header that
downloads models with a progress display.)

### Step 3 — Verify

Still in Command Prompt:
```
ollama list
```
If your model shows up in the list, the AI side is DONE. Everything else is
just opening the tool.

### Step 4 — Open Quest Weaver

Double-click **`run.bat`** in the repo folder (serves the tool at
http://localhost:9999 and opens your browser). Badge green → weave quests.

---

## What to actually test

1. Click through the **player presets** (paladin → goblin rogue → necromancer)
   and **✦ Weave Quest** for each — same quest, completely different reception.
2. Click the **trait tags** (dragonslayer, ex-convict, cursed…) and re-weave.
3. Edit the **NPC personality** and watch the 3D blocky avatar + the dialogue
   both change.
4. Try **Weave for all presets** for the side-by-side comparison.
5. If your PC has a decent GPU: open the **⚡ lab**, crank the cube slider to
   game-like FPS, weave, and watch the performance graph.
6. Model comparison: use the header dropdown + **⬇ pull** to grab a second
   model and weave the same quest with both.

**Unity testers:** import `unity/QuestWeaver.unitypackage` into a Unity
2021.3+ project, make an empty scene, add the `QuestWeaverDemo` component to an
empty GameObject, press Play. WASD to walk, **E** to talk to the three NPCs,
**F5** runs the speed benchmark. Details in `unity/QuestWeaver/README.md`.

---

## Troubleshooting

**Badge says "Ollama offline — mock mode"**
Ollama isn't running. Look for the llama tray icon; if missing, launch
**Ollama** from the Start menu. The page rechecks every 15 s. (Mock mode still
demos the UI with canned text — cards say MOCK.)

**Badge says "model not pulled"**
Click the **⬇ pull** button next to the model dropdown, or run the
`ollama pull …` command from Step 2.

**`ollama` is "not recognized" right after installing**
Your terminal predates the install. Close it and open a fresh one.

**First generation is slow, later ones are fast**
Normal — the first request loads the model into memory (seconds to ~a minute
depending on model size and disk). After that it stays loaded and responses
stream quickly.

**The page opens but generation fails with a CORS error (F12 console)**
Quit Ollama (tray icon → Quit), then in Command Prompt:
```
set OLLAMA_ORIGINS=*
ollama serve
```
Leave that window open and refresh the page.

**Port already in use when starting the tool**
`run.bat` uses port 9999. If something on your machine owns 9999, edit
`run.bat` and change both 9999s to any free port.

**Low disk space on C:**
Models install to `C:\Users\<you>\.ollama` by default. To keep them on another
drive, set a permanent environment variable **before** pulling:
```powershell
[Environment]::SetEnvironmentVariable("OLLAMA_MODELS", "D:\Ollama\models", "User")
```
then restart Ollama (tray → Quit, relaunch) and pull in a **new** terminal.

**Uninstalling everything later**
Windows Settings → Apps → Ollama → Uninstall (it asks whether to keep the
downloaded models). Delete the repo folder. That's the whole footprint.

---

## Mac / Linux

- **Mac:** download Ollama from <https://ollama.com/download> (drag to
  Applications), then in Terminal: `ollama pull qwen2.5:0.5b`. Serve the tool
  with `python3 -m http.server 9999` in the repo folder →
  http://localhost:9999.
- **Linux:** `curl -fsSL https://ollama.com/install.sh | sh`, then the same
  pull + serve commands.

Questions or something broken that isn't listed here? Tell the person who sent
you this link — the list grows with every tester.
