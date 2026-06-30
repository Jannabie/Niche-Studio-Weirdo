



# Tsukihime Remake Parser Tools


---

## What Is This?

`script_text.mrg` is an MZP (`mrgd00`) archive file that stores all the game's dialogue text in UTF-8 encoding. This tool extracts the archive contents into editable `.txt` files, and then repacks them back into the game-compatible MRG format.

The extracted results are organized by route for easier navigation: **Common Route**, **Arcueid Route**, **Ciel Route**, and **QA**. Each text line in the extracted file is associated with its unique offset ID so that the repacking process can precisely recalculate all pointers without breaking the engine's internal structure.

---

## Important Notes

* **Game Dump:** The game must be dumped independently beforehand to obtain the necessary files.
* **Game Version:** This tool can only be used and functions with the **Japanese Version**.

---

## Patch Result Comparison

---

## File Structure

| File | Role |
| --- | --- |
| `mrg_tool.py` | Main tool — extract and repack MRG via GUI or CLI |
| `mrg_editor.py` | Simple text editor for the extracted files |
| `scene_map.json` | Offset-to-scene-name map, required for per-route organization |

---

## How to Use

### GUI Mode

Run it directly to open the graphical user interface:

```bash
python mrg_tool.py

```

Open `script_text.mrg` using the provided button, choose the output folder, and click Extract. Once the `.txt` files are edited, reopen the tool and use the Repack function to generate a new MRG file.

### CLI Mode

```bash
# Extract MRG to a text folder
python mrg_tool.py extract script_text.mrg output/

# Repack the text folder back to MRG
python mrg_tool.py repack output/ script_text_patched.mrg

```

---

## Applying the Patch to the Game (LayeredFS)

Place the repacked `script_text.mrg` into the following path depending on your emulator, without modifying the original ROM files:

**Yuzu:** `%AppData%\Roaming\yuzu\load\010064101344A000\[Mod Name]\romfs\script\`

**Ryujinx:** `%AppData%\Roaming\Ryujinx\mods\contents\010064101344a000\[Mod Name]\romfs\script\`

---

## Requirements

Python 3.8 or newer. `tkinter` is required for GUI mode and comes pre-installed with Python on Windows.

---

## Disclaimer

This tool is created for educational and personal localization purposes. Please use it in compliance with the copyright regulations and Terms of Service of the original game.
