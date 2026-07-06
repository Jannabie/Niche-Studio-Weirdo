# TYPE-MOON Engine (Melty Blood 2002)

**Tab Name:** TYPE-MOON  
**Powered by:** TYPE-MOON Archive Tools (bundled)  
**File Formats:** `.p` (archives), `.TXT` (scripts)

---

## Supported Games

| Game | Notes |
|---|---|
| Melty Blood (2002, original) | `.p` archive format, `.TXT` scripts |
| Other early TYPE-MOON titles | `.p` archive format |

---

## Overview

The early TYPE-MOON engine (as used in the original 2002 Melty Blood) stores all game resources in `.p` archives. Script files inside are plain `.TXT` files. A critical constraint of this engine is that **all in-game text must use fullwidth (Zenkaku) characters** — half-width ASCII characters will corrupt the displayed text.

This tab has **two independent sections**:
- **Archive** — unpack and repack `.p` archives
- **Fullwidth Text Converter** — convert half-width ASCII to fullwidth Zenkaku

---

## Section A — P Archive (Unpack & Repack)

### Step 1 — Unpack .P Archive

1. Under **ARCHIVE**, click **Browse** next to **INPUT .P FILE** and select the `.p` archive from your game directory.
2. Click **Unpack .P**.
3. All files are extracted to a folder next to the archive.

### Step 2 — Repack Folder → .P

1. After editing your extracted files (translating `.TXT` scripts, etc.), click **Browse** next to **INPUT FOLDER** and select the extracted folder.
2. Click **Repack .P**.
3. A new `.p` archive is created.
4. Replace the original `.p` in your game directory and test.

---

## Section B — Fullwidth Text Converter

> ⚠️ **CRITICAL — Half-width ASCII corrupts game text.** The TYPE-MOON engine does not render standard half-width Latin characters. All English text must be converted to fullwidth (Zenkaku) equivalents before inserting into script files. Failing to do this will produce garbled or invisible text in-game.

### Step 3 — Convert Half-width → Fullwidth

1. Under **FULLWIDTH TEXT CONVERTER**, paste or type your half-width ASCII text into the **INPUT** field.
2. The **OUTPUT** field automatically shows the fullwidth Zenkaku equivalent.
3. Click **Copy** to copy the fullwidth text to your clipboard.
4. Paste the fullwidth text into your `.TXT` script file.

**Example conversion:**

| Half-width (❌ Wrong) | Fullwidth (✅ Correct) |
|---|---|
| `Hello, world!` | `Ｈｅｌｌｏ，　ｗｏｒｌｄ！` |
| `I want to fight.` | `Ｉ　ｗａｎｔ　ｔｏ　ｆｉｇｈｔ．` |

---

## Full Workflow Summary

```
SCRIPT PREPARATION:
① Write your translation in any text editor (half-width ASCII is fine here)
② Paste each line into the FULLWIDTH TEXT CONVERTER
③ Copy the fullwidth output → paste into the .TXT script file

ARCHIVE WORKFLOW:
④ Browse INPUT .P FILE → Click Unpack .P → files extracted
⑤ Replace .TXT scripts with your fullwidth-converted translations
⑥ Browse INPUT FOLDER → Click Repack .P → new archive created
⑦ Replace original .p in game directory → test
```

> ⚠️ **Never use half-width ASCII directly in script files.** Always run your translation through the Fullwidth Text Converter before saving. This is non-negotiable for this engine.
