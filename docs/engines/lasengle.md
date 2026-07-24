# Lasengle Engine (MBTL Hook)

**Tab Name:** Lasengle  
**Hook by:** [MaxAkito / MBTL Community Patch](https://github.com/MaxAkito/MBTL-Community-Patch)  
**Hook source:** [MBTL Hook](https://github.com/Jannabie/Niche-Studio-Weirdo/tree/main/MBTL%20Hook)

**Target language:** English (the hook targets the existing English text in the game)

---

## Supported Games

- MELTY BLOOD: TYPE LUMINA (Lasengle / TYPE-MOON)

---

## Overview

MELTY BLOOD: TYPE LUMINA uses the **Lasengle engine** and already ships with an English release. This hook allows you to modify the existing English text and image assets without unpacking the game's archives.

The hook works by intercepting the engine's text loading at runtime and substituting your modified `.txt` scripts and image files from a dedicated folder.

---

## Setup — Install the Hook

1. Clone or download the hook from GitHub:
   ```
   git clone https://github.com/Jannabie/Niche-Studio-Weirdo.git
   ```
2. Inside the repo, navigate to the `MBTL Hook` folder.
3. Copy **all files** from that folder directly into the **root of your game directory** (the folder containing `MBTL.exe`).

The hook folder contains:
- The hook DLL (intercepts text and image loading at runtime)
- A `script/` folder with `.txt` files containing all game dialogue
- An `images/` folder for custom image replacements

---

## How to Translate

### Modifying Text

1. Open the `script/` folder inside your game directory.
2. Edit the `.txt` files with any text editor (Notepad++, VS Code recommended).
3. The files contain the game's English dialogue — replace lines with your translation.
4. Save the file and launch the game. The hook loads your changes automatically at runtime.

> No repacking or rebuilding required — just edit and save.

### Replacing Images

1. Open the `images/` folder inside your game directory.
2. Overwrite any image file with your replacement (keep the same filename and format).
3. Launch the game — the hook will load your image instead of the original.

---

## Full Workflow Summary

```
① Clone the repo → copy hook files to game directory
② Open the script/ folder → edit .txt files with your translation
③ (Optional) Replace images in images/ with your own assets
④ Launch MBTL.exe and test in-game
```

> **Tip:** You do not need this tool's UI to use the hook — just copy the files and edit the `.txt` scripts directly. This tab exists as a reference and quick-access guide to the hook repository.
