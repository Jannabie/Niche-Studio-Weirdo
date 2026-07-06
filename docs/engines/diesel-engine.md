# Diesel Engine / Nitroplus (NPK)

**Tab Name:** Diesel Engine  
**Powered by:** [MwareStuff / NPK3Tool](https://github.com/marcussacana/MwareStuff)  
**File Formats:** `.npk`, `.nut` (Squirrel bytecode)

---

## Supported Games

| # | Game | Platform |
|---|---|---|
| 0 | You and Me and Her (Jast USA) | PC |
| 1 | You and Me and Her (Steam) | PC |
| 2 | Tokyo Necro | PC |
| 3 | Minikui Mojika no Ko | PC |
| 4 | SoniComi (JastUSA) | PC |
| 5 | The Song of Saya (Steam) | PC |
| 6 | The Song of Saya (Steam) [+18] | PC |
| 7 | Kishin Houkou Demonbane | PC |
| 8 | DRAMAtical Murder (Jast USA) | PC |
| 9 | DRAMAtical Murder (Steam) | PC |
| 10 | Full Metal Daemon Muramasa (Jast USA) | PC |
| 11 | Slow Damage (Jast USA) | PC |
| 12 | Tokyo Necro (Jast USA) | PC |
| 13 | sweet pool (Jast USA) | PC |
| 14 | sweet pool (Steam) | PC |
| 15 | Togainu no Chi –Lost Blood– (Jast USA) | PC |
| 16 | Togainu no Chi –Lost Blood– (Steam) | PC |

>  **Always select the correct game profile first.** Each Nitroplus game uses a different encryption key for its NPK archives. A wrong profile will produce a corrupt extraction.

---

## Overview

The Diesel Engine (used by Nitroplus) stores all game resources — scripts, CG, sound — inside `.npk` archives. Scripts are compiled Squirrel bytecode (`.nut` files). This tab handles both the archive layer and the script layer independently.

---

## Step 1 — Select Game Profile

At the top of the tab, choose the game you are working on from the dropdown. This sets the correct decryption key for all archive operations.

---

## Step 2 — Extract the .NPK Archive

1. Under **EXTRACT**, click `...` next to **INPUT .NPK** and select the `.npk` file from your game directory (e.g. `script.npk`, `cg.npk`).
2. Click `...` next to **OUTPUT DIRECTORY** and pick a folder where the extracted files will be dumped.
3. *(Optional)* Fill in **FILE FILTER** to only extract specific file types (e.g. `.nut` to skip CG and only get scripts — **much faster**).
4. *(Optional)* Check **Skip already extracted files** to resume an interrupted extraction without re-doing work.
5. Click **Extract → Folder**.

---

## Step 3 — Extract Text from .NUT Scripts

Each `.nut` file is compiled Squirrel bytecode containing the dialogue and UI strings.

1. Under **CNUT SCRIPT TRANSLATOR**, click `...` and select the `.nut` file you want to work on.
2. Click **Extract Text → .JSON**. A JSON file will be created next to the `.nut` with all extractable strings.
3. Open the JSON in any text editor (Notepad++ recommended). Translate the values for each key.
4. Click **Inject .JSON → .NUT** to write your translations back into a new `.nut` file.

---

## Step 4 — Repack to .NPK

1. Replace the original `.nut` files in your extracted folder with the translated versions.
2. Under **REPACK**, click `...` next to **INPUT FOLDER** and select the extracted folder.
3. Click `...` next to **OUTPUT .NPK** and choose where to save the new archive.
4. Set **Packing Options** as needed:
   - **Force Segmentation** (default: on) — keep this enabled for most games
   - **Enable Compression** — only enable if the original archive was compressed
5. Click **Repack → .NPK**.
6. Replace the original `.npk` in your game folder with the new file and test.

---

## Full Workflow Summary

```
① Select Game Profile
② Extract NPK (use File Filter ".nut" to only grab scripts — saves time)
③ Extract Text → JSON from each .nut
④ Translate the JSON strings
⑤ Inject JSON → NUT (translated .nut files)
⑥ Replace the translated .nut files into the extracted folder
⑦ Repack → NPK
⑧ Replace the original .npk in your game directory → test
```

> **Always back up your original `.npk` files** before replacing them in the game directory!
