# N2System / Nitro+ (NPA)

**Tab Name:** N2System  
**Powered by:** Custom NPA tool (bundled in `Utility/N2SystemBin/`)  
**File Formats:** `.npa`

---

## Supported Games

| Game | Tag |
|---|---|
| [No Encryption] — Unknown / Other Game | *(no key)* |
| Chaos;Head | ChaosHead |
| Chaos;Head (Trial version) | ChaosHeadTr1 |
| Chaos;Head (Trial version 2) | ChaosHeadTr2 |
| Full Metal Daemon Muramasa | Muramasa |
| Full Metal Daemon Muramasa (Trial) | MuramasaTr |
| Full Metal Daemon Muramasa (After Disk) | MuramasaAD |
| Full Metal Daemon Muramasa: Shokuzai-hen | MuramasaSS |
| Sumaga | Sumaga |
| Sumaga (3rd Party) | Sumaga3P |
| Sumaga SP | SumagaSP |
| Django | Django |
| Django (Trial) | DjangoTr |
| Lamento -Beyond the Void- | Lamento |
| Lamento -Beyond the Void- (Trial) | LamentoTr |
| sweet pool | sweetpool |
| Demonbane | Demonbane |
| Axanael | Axanael |
| Kikokugai | Kikokugai |
| Super Sonico -The Animation- (SoniComi) | Sonicomi |
| Super Sonico -The Animation- (SoniComi, Trial 2) | SonicomiTr2 |
| Phenomeno: Lost X | LostX |
| Phenomeno: Lost X (Trailer) | LostXTrailer |
| DRAMAtical Murder | DRAMAticalMurder |
| DRAMAtical Murder re:connect | DRAMAticalMurderRC |
| Kimi to Kanojo to Kanojo no Koi (Totono) | Totono |

> If your game is not in the list, try **[No Encryption]** first. Some older NPA archives have no encryption.

---

## Overview

N2System is an older Nitroplus engine (predating the Diesel/NPK era) used for games like Django, Chaos;Head, Lamento, and DRAMAtical Murder. Resources are packed into `.npa` archives that are encrypted with a per-game key.

---

## Step 1 — Select Game Profile

Choose your game from the dropdown at the top. This is critical — each game uses its own encryption key. If you choose the wrong one, extraction will silently produce garbled or empty files.

---

## Step 2 — Extract .NPA Archive

1. Under **STEP 1 — EXTRACT**, click `...` next to **INPUT .NPA FILE** and select your archive (e.g. `nscript.npa`, `cg.npa`).
2. Click **Extract → Folder**. The extracted folder is automatically created **next to your `.npa` file** — no need to specify an output location.
3. The console will print a file list. **Garbled/symbol characters in the filenames are normal** — the filenames are in Japanese and your system may not have Japanese locale set.

---

## Step 3 — Edit Your Files

After extraction, open the script files (usually `.txt` or proprietary format) and translate them.

>  **Escape Character Rule** — the N2System engine cannot parse certain punctuation directly. You must prefix them with `&` (ampersand):
>
> | Character | How to Write |
> |---|---|
> | `.` (period) | `&.` |
> | `,` (comma) | `&,` |
> | `!` (exclamation) | `&!` |
> | `?` (question mark) | `&?` |
> | `…` (ellipsis) | `&…` |
>
> **Example:**  
>  WRONG: `I want to eat.`  
> CORRECT: `I want to eat&.`

---

## Step 4 — Repack .NPA Archive

1. Under **STEP 2 — REPACK**, click `...` next to **INPUT FOLDER** and select the extracted folder you edited.
2. Click `...` next to **OUTPUT .NPA** and choose where to save the new archive.
3. **Compression Option:**
   -  Enable compression only for **script/data** archives
   -  **NEVER enable compression for CG/image archives** — it will corrupt the graphics and break the game
4. Click **Repack → .NPA**.
5. Replace the original `.npa` in your game directory with the new one and test.

---

## Full Workflow Summary

```
① Select Game Profile
② Extract NPA archive → auto-folder next to the .npa file
③ Translate the script files (mind the & escape rule!)
④ Repack → NPA (DO NOT compress CG folders)
⑤ Replace the original .npa in your game directory → test
```

