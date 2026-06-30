# HuneX (Tsukihime Remake)

> Extract script text from `script_text.mrg`, translate it, then repack.

**Example games:** Tsukihime -A Piece of Blue Glass Moon- (Remake)

---

## Tools Available

### Extract & Repack (`script_text.mrg`)

The `script_text.mrg` file contains all in-game dialogue and text.

---

### EXTRACT — `script_text.mrg` → `.txt`

1. Browse → select your `script_text.mrg` file
2. Browse → choose where to save the output `.txt`
3. Click **Extract → .TXT**

The output `.txt` contains all the game's dialogue, one line per entry.

---

### Translate

- Open the `.txt` in Notepad++
- Translate each Japanese line to your target language
- Save the file (keep UTF-8 encoding)

---

### REPACK — `.txt` → `script_text.mrg`

1. Browse → select your **edited** `.txt` file
2. Browse → choose the output `.mrg` path
3. Click **Repack → .MRG**
4. Replace `script_text.mrg` in your game data folder

> **Warning:** Keep the number of lines identical to the original. Adding or removing lines will break the repack.
