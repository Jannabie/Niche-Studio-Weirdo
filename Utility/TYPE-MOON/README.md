# Melty Blood 2002 Archive Tools & Editor

A tool for modding and localizing **Melty Blood (2002)** — from extracting archives, editing text through a GUI, to repacking back into the original format.

**Requirements:** Python 3.10+, no external deps (except `tkinter` for the GUI, usually already included with Python on Windows).

---
## Repo Contents
| File | Purpose |
|---|---|
| `mb_core.py` | Core library + CLI for unpacking/repacking `.p` archives |
| `mb_editor.py` | GUI editor for translators — no need to open a terminal |

Both are compatible with the *Mirror Moon English Patch* archive format (Shift-JIS).

---
## How to Use

### GUI — Recommended for Translators
```bash
python mb_editor.py
```
Click **Open Archive (.p)** → select `data04.p`. The left panel will display all script files inside the archive. Select one, enter your translation in the box that appears for each dialogue line, then click **Repack Archive** when done.

Available features: syntax highlighting (to easily distinguish commands from text), global find & replace, progress tracking, and translation export/import for team collaboration.

### CLI — For Automation
```bash
# Extract archive into a folder
python mb_core.py unpack data04.p extracted/

# Repack the extracted folder
python mb_core.py repack extracted/ data04_new.p

# View archive contents (offset, size, etc.) without extracting
python mb_core.py info data04.p
```

---
##  Must Read: Fullwidth Characters
This game uses a Shift-JIS font renderer that **doesn't understand regular Latin (half-width) characters**. If you write a translation using regular `A`, `B`, `C`, the text won't appear in the game at all — or it will appear as corrupted characters.

All translations must use **Fullwidth (Zenkaku) characters**:

```
 Half-width : "At the beginning of August."
 Fullwidth  : "Ａｔ　ｔｈｅ　ｂｅｇｉｎｎｉｎｇ　ｏｆ　Ａｕｇｕｓｔ．"
```

The easiest way is to switch your IME input mode to Zenkaku, or use a conversion table. The GUI editor displays the text as-is, so you can directly control the output.

---
## About `_manifest.json`
Every time an archive is extracted, a `_manifest.json` file is automatically created in the extraction output folder. This file stores the original file order and header flags — two things required so that repacking can produce an archive identical byte-for-byte to the original.

**Do not delete or modify this file.** Without `_manifest.json`, repacking won't work.

---
## `data04.p` Structure
The main archive file that needs to be edited for localization. Total of 189 files (~40 MB):

| Type | Count | Content |
|---|---|---|
| `.TXT` | 62 | Dialogue script (±10,676 lines) — **needs to be edited** |
| `.EX3` | 107 | Image/sprite data |
| `.WAV` | 9 | Audio |
| `.FNT` | 1 | Game font data |

Files other than `.TXT` don't need to be touched unless you want to mod graphics or audio.

---
## Script Format
`.TXT` files use an internal script format. Only the dialogue/narration lines may be translated:

- `// ...` — comment, **skip**
- `EF`, `GW`, `WI`, `MD`, `BP`, etc. — game commands, **do not modify**
- Lines starting with a space or Japanese characters — **these are the ones to translate**

---
## Proof of Concept
| Screenshot |
|:---:|
| ![Editor in action](https://i.imgur.com/UEhFLTl.png) |
| *GUI editor running with translation applied* |

---
## Disclaimer
This tool is created for educational, research, and personal localization purposes. Ensure its use complies with the copyright rules and ToS of the original game.
