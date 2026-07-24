# rUGP Engine (Schwarzesmarken Hook)

**Tab Name:** rUGP Engine  
**Hook by:** Yuza  
**Hook source:** [Schwarzesmarken Hook](https://github.com/Jannabie/Niche-Studio-Weirdo/tree/main/Schwarzesmarken%20Hook)

**File Formats:** `.jlx` (script), `.json` (extracted text)

---

## Supported Games

- Schwarzesmarken (âge / ACID STUDIO)
- Other rUGP engine titles may be compatible

---

## Overview

The rUGP engine stores its dialogue in `.jlx` files encoded in **UTF-16LE** with a `::::: ` delimiter format. This tab provides a full parse-and-repack pipeline:

1. **Parse** `orgi.jlx` + `trans.jlx` → clean JSON array for editing
2. **Translate** the JSON file using any text editor or AI tool
3. **Repack** the translated JSON → `trans.jlx` for in-game use

Translation is applied via a runtime text hook (`winmm.dll`) — no binary patching required.

---

## Setup — Install the Hook

1. Clone or download the hook from GitHub:
   ```
   git clone https://github.com/Jannabie/Niche-Studio-Weirdo.git
   ```
2. Inside the repo, navigate to the `Schwarzesmarken Hook` folder.
3. Copy **all files** from that folder directly into the **root of your game directory** (the folder containing the main game `.exe`).
4. The key files are:
   - `winmm.dll` — the hook DLL (intercepts text at runtime)
   - `orgi.jlx` — the original Japanese script
   - `trans.jlx` — the translation file (edit this one)

---

## Step 1 — Parse JLX → JSON

1. Under **STEP 2 — PARSE**, select your **orgi.jlx** (original Japanese script).
2. Select your **trans.jlx** (translation target file).
3. Set an **output JSON** path (e.g., `translation.json`).
4. Click **Parse JLX → JSON**.

The output is a UTF-8 JSON array. Each entry looks like:

```json
[
  {
    "jp": "Original Japanese text here.",
    "tl": ""
  }
]
```

> **Note:** Any `\u0003` control characters found in the original `.jlx` are automatically cleaned during parsing. This is safe — the hook handles formatting independently.

---

## Step 2 — Translate

Open the output `.json` file in any text editor (Notepad++, VS Code, etc.) or paste it into an AI translation tool.

Fill in the `"tl"` fields with your translations:

```json
[
  {
    "jp": "Original Japanese text here.",
    "tl": "Your English translation here."
  }
]
```

**Rules:**
- Only edit the `"tl"` values — do **not** modify the `"jp"` values.
- Do **not** add or remove entries — the entry count must match `orgi.jlx` exactly.

---

## Step 3 — Repack JSON → trans.jlx

1. Under **STEP 3 — REPACK**, select your translated `.json` file.
2. Set the output path for `trans.jlx`.
3. Click **Repack JSON → JLX**.
4. Copy the resulting `trans.jlx` into your game directory alongside `winmm.dll`.

---

## Step 4 — Test

Launch the game normally. The hook will intercept text at runtime and replace it with your translations from `trans.jlx`.

---

## Full Workflow Summary

```
① Clone the repo → copy hook files to game directory
② Parse orgi.jlx + trans.jlx → JSON
③ Fill in the "tl" fields with your translations
④ Repack JSON → trans.jlx
⑤ Place trans.jlx in the game directory
⑥ Launch the game and test
```
