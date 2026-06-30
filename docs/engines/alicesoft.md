# Alicesoft Engine

> Tools for Alicesoft games using [alice-tools](https://github.com/nunuhara/alice-tools) by nunuhara.

**Example games:** Rance 10, Rance IX, Evenicle, Dungeons & Dolls

---

## Tools Available

### 1. Archive Tools (`.afa`, `.ald`, `.dat`)

Used to pack/unpack Alicesoft archive files that contain all game assets.

| Button | Action |
|---|---|
| **List** | Show all files inside the archive |
| **Extract** | Unpack the archive into a folder |
| **Pack** | Repack a folder back into an archive |

**How to use:**
1. Browse → select your `.afa` or `.ald` archive file
2. Browse → select a folder where you want to extract (or pack from)
3. Click **Extract** to unpack, or **Pack** to repack

---

### 2. Script Tools (`.ain`)

Used to dump and re-inject **dialogue and script text** from the main game script.

| Button | Action |
|---|---|
| **Dump** | Extract all text from the `.ain` file into a `.txt` |
| **Edit** | Inject translated text back into a new `.ain` file |

**How to use:**

**Step 1 — Dump:**
1. Browse → select your `.ain` file (e.g. `Rance10.ain`)
2. Click **Dump** → save the output as a `.txt` file (e.g. `rance10_dump.txt`)

**Step 2 — Translate:**
- Open the `.txt` file in Notepad++
- The format looks like this:
  ```
  ; FunctionName@SubFunction
  ;m[12345] = "「Japanese text here.」"
  ```
- To translate a line, **you do NOT need to remove the semicolon** — Niche Studio does it automatically
- Just find the Japanese text and replace it with your translation:
  ```
  ;m[12345] = "「Your translated text here.」"
  ```
- Lines starting with `; FunctionName` are comments — **do not touch them**

**Step 3 — Inject:**
1. Browse (top field) → select the **original** `.ain` file
2. Browse (bottom field) → select your **translated** `.txt` file
3. Click **Edit** → save the output as a new `.ain` file
4. Replace the original `.ain` in your game folder with the new one

> **Note:** The `s[...]` entries are string constants used by the game engine.  
> The `m[...]` entries are the actual dialogue lines shown to the player.

---

### 3. Database Tools (`.ex`)

Used to dump and rebuild **character names, item names, skills**, and other database strings.

> In Rance 10, this is the file `Rance10EX.ex`. It contains full character names like `シィル・プライン` that do NOT appear in the `.ain` script.

| Button | Action |
|---|---|
| **Dump** | Extract the database into a human-readable `.x` text file |
| **Edit** | Rebuild a new `.ex` from your edited `.x` text file |

**How to use:**

**Step 1 — Dump:**
1. Browse (top field) → select your `.ex` file (e.g. `Rance10EX.ex`)
2. Click **Dump** → save the output as a `.x` or `.txt` file

**Step 2 — Translate:**
- Open the file. It uses a C-like tree structure:
  ```
  tree キャラクター情報 = {
      "Lv35 シィル" = {
          フルネーム = "シィル・プライン",
          ...
      },
  };
  ```
- Also contains the character name lookup table:
  ```
  { "シィル／", "シィル・プライン" },
  ```
- Edit the **right-hand side values** (the strings after `=`) with your translations
- **Do NOT change the structure, brackets, or commas**

**Step 3 — Rebuild:**
1. Browse (bottom field) → select your translated `.x` / `.txt` file
2. Click **Edit** → save output as a new `.ex` file
3. Replace the original `.ex` in your game folder with the new one

> **Important:** Unlike `.ain` editing, you do NOT need the original `.ex` for the rebuild — the text file is self-contained.

---

### 4. Image Tools (`.cg`)

Used to convert Alicesoft `.cg` images to/from standard formats.

| Button | Action |
|---|---|
| **Convert Image** | Auto-detects direction: `.cg` → `.png`/`.webp`, or `.png`/`.webp` → `.cg` |

**How to use:**
1. Browse → select your image file (`.cg`, `.png`, or `.webp`)
2. Click **Convert Image** → select the output path and format

---

## File Reference

| File | Contains |
|---|---|
| `Rance10.ain` | All dialogue, script logic |
| `Rance10EX.ex` | Character names, item names, skills, database |
| `*.afa` | Packed game assets (sprites, audio, etc.) |
| `*.ald` | Older packed archives (pre-System 4) |
