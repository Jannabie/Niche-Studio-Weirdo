# MalieToolKit

A collection of tools for extracting, editing, and repacking resources from games running on the **FreeMalie / Malie Engine** (by light).

> Tested on: **Soushuu Senshinkan Gakuen: Hachimyoujin** (相州戦神館學園 八命陣)

> **NOTE: For Kajiri Kamui Kagura (神咒神威神楽 / KKK):** This game has its **own exclusive toolkit** due to differences in its archive structure and unique script format compared to other Malie games.
> Use the dedicated repo instead: **[MalieKit — KKK Exclusive Toolkit](https://github.com/Jannabie/KKK)**

---

## Toolkit Contents

| Folder | Contents | Description |
|---|---|---|
| `LauncherDatSource/` | Python CLI source | UnPacker / RePacker for `.dat` / `.lib` |
| `MalieExScSource/` | C# Script Tool source | Extract & repack dialogue scripts |
| `MalieScriptExtractor/` | `Malie_Script_Tool.exe` | Prebuilt version of the script tool |

---

## Part 1 — Data Archive Tool (`.dat` / `.lib`)

This tool is used to open and repack the game's main archives, such as `data.dat`, `sound.dat`, etc.

### Requirements

- Python 3.10+
- Dependency: `tqdm`

```bash
pip install tqdm
```

### Running the CLI

```bash
cd LauncherDatSource
python cli_launcher.py
```

Or use the prebuilt version, downloadable at:

**[Releases v1.0 → Malie_UnRePacker_Tool_CLI.exe](https://github.com/Jannabie/MalieToolKit/releases/tag/v1.0)**

### CLI Menu

```
[1] Stage 1 decryption (.dat only)
[2] Full extraction (.lib/.dat)
[3] Plain repack (.dat only)
[4] MGF ↔ PNG conversion
[Q] Quit
```

---

### Menu Explanation

#### [1] Stage 1 Decryption
Decrypts an encrypted `.dat` file into a `_plain.dat` file (contents not yet extracted).
Useful for peeking at the raw contents before doing a full extraction.

**Input:** path to the `.dat` file
**Output:** `filename_plain.dat` in the same location

---

#### [2] Full Extraction
Opens a `.dat` or `.lib` archive and extracts all of its contents.
Automatically generates a `filename_entries.json` metadata file, which **must be kept** for repacking purposes.

**Input:** path to the `.dat` / `.lib` file, output folder path
**Output:** all extracted files + `filename_entries.json`

Supported formats:

| Format | Description |
|---|---|
| `.ogg` | Audio, decrypted directly |
| `.png` / `.pn` | PNG images |
| `.mgf` | Malie's custom image format, saved as-is |
| `.dzi` | Tiled image metadata |
| `.svg` | Vector graphics |
| `.csv` / `.txt` | Text / data |
| `.mpg` | Video |
| `.swf` | Flash |
| Others | Saved as-is (including `exec.dat`, etc.) |

---

#### [3] Plain Repack
Reassembles edited files back into a `.dat` archive.

> NOTE: Only supports plain (unencrypted) `.dat` files. Repacking encrypted `.dat` files is not yet supported.

**Input:**
- Folder containing the source files
- Output `.dat` filename
- Path to the metadata `.json` file (from the extraction step)

---

#### [4] MGF ↔ PNG Conversion
Converts images between Malie's `.mgf` format and standard `.png`.

**MGF → PNG:**

```bash
# Via the interactive CLI, choose [4], or directly:
python execution/mgfpng_change.py filename.mgf --to-png
```

**PNG → MGF:**

```bash
python execution/mgfpng_change.py filename.png --to-mgf
```

---

## Part 2 — Script Tool (Dialogue & Strings)

This tool is used to extract and import dialogue text and character names from the game's script file (typically `exec.dat` inside the `system/` folder).

### Requirements

- .NET 8 Runtime (to run the `.exe`)
- Or .NET 8 SDK (to build from source in `MalieExScSource/`)

### Building from Source

```bash
cd MalieExScSource
dotnet build
```

Or if the dotnet path isn't detected automatically:

```bash
"C:\Program Files\dotnet\dotnet.exe" build
```

---

### CLI Commands

```
Malie_Script_Tool.exe -d  -in [input.dat] -out [output.txt]        → Disassemble script
Malie_Script_Tool.exe -a  -in [input.dat] -out [output.txt]        → Export all strings (including character names)
Malie_Script_Tool.exe -s  -in [input.dat] -out [output.dat] -txt [input.txt]  → Import strings
Malie_Script_Tool.exe -e  -in [input.dat] -out [output.txt]        → Export dialogue
Malie_Script_Tool.exe -i  -in [input.dat] -out [output.dat] -txt [input.txt]  → Import dialogue
```

---

### Text File Format

Each entry has two lines:

```
◇00000000◇Original text (do not change)
◆00000000◆Translated text (edit here)
```

---

### IMPORTANT: How to Change Character Names in the Malie Engine

In the Malie Engine, character names are **NOT stored in the regular dialogue segment**, but rather in the string segment. If you export dialogue directly (`-e`), edit the names there, and then import (`-i`), the character names **will not change** in-game.

**Correct order:**

```
1. Export strings first
   Malie_Script_Tool.exe -a -in exec.dat -out exec_strings.txt

2. Edit character names in exec_strings.txt
   Find the ◆ lines containing the original names, replace with the translated names

3. Import strings (repack character names)
   Malie_Script_Tool.exe -s -in exec.dat -out exec_patched.dat -txt exec_strings.txt

4. Use exec_patched.dat as the input for the next step

5. Export dialogue from the already-patched file
   Malie_Script_Tool.exe -e -in exec_patched.dat -out exec_dialog.txt

6. Edit dialogue in exec_dialog.txt

7. Import dialogue (repack dialogue)
   Malie_Script_Tool.exe -i -in exec_patched.dat -out exec_final.dat -txt exec_dialog.txt

8. Replace exec.dat in the game folder with exec_final.dat
```

---

## Full Workflow (End-to-End)

```
[Original game files]
       │
       ▼
[1] Extract data.dat  →  folder containing exec.dat, images, audio, etc.
       │
       ▼
[2] Patch character names in exec.dat  (-a → edit → -s)
       │
       ▼
[3] Extract & edit dialogue  (-e → edit → -i)
       │
       ▼
[4] Repack the folder into a new .dat  (CLI menu [3])
       │
       ▼
[Patched game files]
```

---

## Testing Evidence

This toolkit has been tested on:

**Soushuu Senshinkan Gakuen: Hachimyoujin**
相州戦神館學園 八命陣

| Item | Detail |
|---|---|
| Developer | light |
| Engine | FreeMalie |
| Testing | Archive extraction, character name patching, dialogue patching |
| Status | Successful |

---

>  **Kajiri Kamui Kagura (神咒神威神楽 / KKK)** has not been tested with this toolkit — KKK has its **own exclusive toolkit** in a separate repo due to the uniqueness of its archive structure and script format.
> → **[MalieKit — KKK Exclusive Toolkit](https://github.com/Jannabie/KKK)**

**Translation result screenshot:**

| Soushuu Senshinkan Gakuen: Hachimyoujin (相州戦神館學園 八命陣) |
|:---:|
| ![Translation working in-game](https://i.imgur.com/WLKLpyp.jpeg) |

---

## Notes

- Repacking **encrypted** `.dat` files is not yet supported — plain `.dat` repack results may not be readable by certain games that use full encryption.
- The `filename_entries.json` file generated during extraction **must be kept** and used during repacking to ensure the archive structure remains valid.
- This tool is a modification of [Malie_Script_Tool](https://github.com/crskycode/Malie_Script_Tool) by crskycode, with added support for importing strings (`-s`) for character name replacement.

---

## License

This project is open-source for the purposes of preservation and fan translation.
Credit: crskycode (original Malie_Script_Tool), modifications & toolkit by this project's contributors.
