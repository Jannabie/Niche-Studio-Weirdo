# Minori Engine

**Tab Name:** Minori  
**Powered by:** Minori PAZ Tools (bundled)  
**File Formats:** `.paz` (archives)

---

## Supported Games

| Game Index | Game | Notes |
|---|---|---|
| 0 | ef – a fairy tale of the two. (First Tale, JP) | Japanese version |
| 1 | ef – a fairy tale of the two. (First Tale, EN) | English version |
| 7 | eden* THEY WERE ONLY TWO, ON THE PLANET (JP) | Japanese version |
| 8 | eden* THEY WERE ONLY TWO, ON THE PLANET (EN, Steam) | Steam English version |
| 10 | Mashiro-iro Symphony -The color of lovers- (JP) | Japanese version |
| — | Supipara | See community resources for index |

>  **Selecting the wrong Game Index is the most common mistake with this engine.** Each game uses a different `.paz` encryption key tied to its Game Index. A wrong selection will produce empty or corrupted output.

---

## Overview

The Minori engine stores all game resources — scripts, CG, audio — inside encrypted `.paz` archives. Each game uses a unique encryption key identified by its **Game Index** number. You must select the correct Game Index before performing any archive operations.

---

## Step 1 — Select Game Index

1. At the top of the tab, choose your game from the **GAME INDEX** dropdown.
2. Use the table above to find the correct index number for your game and version.

>  **The Game Index is tied to both the game AND its version (JP vs EN, retail vs Steam).** Make sure to select the index that matches your exact copy of the game.

---

## Step 2 — Select Input Archive

1. Click **Browse** next to **INPUT .PAZ FILE** and select the `.paz` archive from your game directory.

---

## Step 3 — Select Output Directory

1. Click **Browse** next to **OUTPUT DIRECTORY** and choose an empty folder where extracted files will be saved.

---

## Step 4 — Unpack .PAZ

1. Click **Unpack .paz**.
2. The tool decrypts and extracts all files from the archive into the output directory.
3. Edit the extracted files (translate scripts, edit images, etc.) and save.

---

## Step 5 — Repack Folder → .PAZ

1. After editing, click **Repack Folder**.
2. The tool repacks all files from the output directory back into a new `.paz` archive, re-encrypting with the correct key for the selected Game Index.
3. Replace the original `.paz` in your game directory with the new file and test.

---

## Full Workflow Summary

```
① Select correct GAME INDEX from dropdown (critical!)
② Browse INPUT .PAZ FILE
③ Browse OUTPUT DIRECTORY (empty folder)
④ Click Unpack .paz → files decrypted and extracted
⑤ Translate/edit the extracted files
⑥ Click Repack Folder → new .paz archive created
⑦ Replace original .paz in game directory → test
```

