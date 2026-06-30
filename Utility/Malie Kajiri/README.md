# Kajiri Kamui Kagura — ToolKit

Tools for translating Kajiri Kamui Kagura using the Malie Script Tool.

---

## Translation Status

| Component | Status |
|----------|--------|
| Main dialogue (`exec.msg.txt`) | Editable |
| Menu & names (`exec.str.txt`) | Editable |
| Title Screen / UI Frame | Soon |
| Toolkit Patching | Done |

| Kajiri Kamui Kagura Akebono no Hikari (神咒神威神楽 曙之光 ダウンロード版) |
|:---:|
| ![Translation working in-game](https://i.imgur.com/x4kWxLV.jpeg) |

| Horizontal Patch |
|:---:|
| ![Horizontal](https://i.imgur.com/9PHxZ8Z.jpeg) |

| Recommended Font | Notes |
|------------------|------------|
| MS ゴシック | Default font |
| Grisaia Custom SP | — |
| Grisaia Custom | — |
| MotoyaLMaruM | — |
| LUNE | — |
| BIZ UD明朝 Medium | — |

---

## Two Patch Variants

There are two repo folders, each for a different text mode:

| | Vertical | Horizontal |
|--|--|--|
| Text Window | Straight ADV | Sideways ADV |
| messageframe folder | SVG modified per type | All use `normal` |
| Editing character names | `exec.str.txt` | `exec.str.txt` |
| Editing dialogue | `exec.msg.txt` | `exec.msg.txt` |

The workflow is identical — the only difference is the messageframe folder that gets copied to the game.

---

## Required Before You Start

**1. Use `malie.exe` and `malie.ini` from this repo**

```
[KT] KKK\
├── malie.exe     ← launch the game from here (no need for AlphaROMdiE)
└── malie.ini     ← copy into the game's installation folder
```

**2. Manually create the `.data\system` folder** *(only needed for the Vertical repo — already present in Horizontal)*

```bat
mkdir "dependencies\malie tools\compilar\Malie_Script_Tool-main\bin\Debug\.data\system"
```

---

## Requirements

- **Python** — [python.org](https://python.org), check **"Add to PATH"** during installation
- **Notepad++** — [notepad-plus-plus.org](https://notepad-plus-plus.org)
- KKK game already installed

---

## File Format

### `exec.msg.txt` — Main Dialogue

Open with Notepad++, make sure the encoding is **UTF-8** (not UTF-8-BOM).

```
◇00000002◇　振り下ろす一閃――[z]
◆00000002◆　A slash that comes crashing down――[z]
```

| Symbol | Function |
|--------|--------|
| `◇` | Original Japanese text — do not change |
| `◆` | Translation line — this is what you edit |
| `[z]` | End-of-dialogue marker, must be present |
| `[c]` | Pause, waits for player click |
| `[n]` | Manual line break |
| `[s]` | Voice/sound marker |

Rules: only edit `◆` lines, do not remove the `[z]` `[c]` `[n]` `[s]` markers, do not change the ID numbers.

---

### `exec.str.txt` — Character Names & UI Strings

This file contains character names, menu options, and other interface strings. Edit the `◇` line directly to replace the Japanese text with the translation — the change will show up directly in the game.

Example:

```
◇00006A3E◇覇吐
```

Change to:

```
◇00006A3E◇Habaki
```

> **Note:** Unlike `exec.msg.txt`, this file doesn't use `◇`/`◆` pairs. Edit the value directly after `◇XXXXXXXX◇`.

After editing, the compile and pack process is the same as usual (see the section below).

---

## Workflow

Make sure your CMD is in the `dependencies\` folder:

```bat
cd "C:\Users\user\Downloads\KKK exe manipulator\KKK-main\dependencies"
```

### Option A — Direct Editing (No Wordwrap)

Edit this file directly:

```
dependencies\malie tools\compilar\Malie_Script_Tool-main\bin\Debug\data\system\exec.msg.txt
```

Try to keep lines ≤25 characters. Use `[n]` to break them:

```
◆00000003◆　That attack wasn't just[n]an ordinary slash――[z]
```

---

### Option B — Via the `script` Folder + Automatic Wordwrap

For long lines you want `wordwrap.py` to break automatically.

One-time setup:

```bat
mkdir "dependencies\script"
```

Workflow:

1. Copy `exec.msg.txt` into `dependencies\script\`, rename it to `message.txt`
2. Edit `message.txt` with Notepad++
3. Run wordwrap:
   ```bat
   python wordwrap.py
   ```
4. Copy the result to the compile tool:
   ```bat
   copy "script_done\message.txt" "malie tools\compilar\Malie_Script_Tool-main\bin\Debug\data\system\exec.msg.txt"
   ```

---

## Compile & Pack

### Step 1 — Compile the script

```bat
"malie tools\compilar\Malie_Script_Tool-main\bin\Debug\Malie_Script_Tool.exe"
```

Output: `exec.dat` in `.data\system\exec.dat`

If a `DirectoryNotFoundException` appears → create the `.data\system` folder first (see **Required Before You Start**).

### Step 2 — Pack into `data6.dat`

```bat
python dat_pack.py "C:\Users\user\Downloads\KKK exe manipulator\KKK-main\data"
```

Always provide the full path to the `data` folder. The `data6.dat` file appears in the `dependencies\` folder.

### Step 3 — Install into the game

```bat
copy data6.dat "C:\[game installation folder]\data6.dat"
```

---

## Patch Installation (One-Time)

Besides `data6.dat`, these two things need to be copied into the game folder once at the start:

**`malie.ini`** — copy from `[KT] KKK\malie.ini` into the game's root folder, overwriting the existing one.

**`messageframe` folder** — copy the entire contents from `data\screen\messageframe\` to `[game folder]\data\screen\messageframe\`. Overwrite all SVG files.

> For the **Horizontal Patch**, use the `messageframe` folder from the Horizontal repo. All the SVGs in it are already configured with the `normal` type (horizontal text).

---

## Troubleshooting

**`python: can't open file 'dat_pack.py'`**
CMD is in the wrong folder. Move into `dependencies\`.

**`PermissionError: [WinError 5]` when running `dat_pack.py`**
Running it without a path argument will open a dialog and you might select the wrong folder. Always use an explicit path:
```bat
python dat_pack.py "C:\...\KKK-main\data"
```

**`DirectoryNotFoundException: .data\system\exec.dat`**
The `.data\system\` folder doesn't exist yet. Create it first:
```bat
mkdir "malie tools\compilar\Malie_Script_Tool-main\bin\Debug\.data\system"
```

**Text in-game is still Japanese after patching**
Make sure `data6.dat` was copied to the correct game installation folder and that you're using `malie.exe` from `[KT] KKK\`.

---

## Folder Structure

```
KKK-main\
│
├── [KT] KKK\
│   ├── malie.exe              ← launch the game from here
│   └── malie.ini               ← copied into the game folder once
│
├── data\
│   └── screen\
│       └── messageframe\      ← dialogue box SVGs (copied into the game once)
│
└── dependencies\
    │
    ├── wordwrap.py            ← automatically break long lines (optional)
    ├── dat_pack.py            ← pack into data6.dat
    │
    ├── script\                ← create if using Option B
    │   └── message.txt
    ├── script_done\           ← wordwrap output (Option B)
    │
    └── malie tools\
        └── compilar\
            └── Malie_Script_Tool-main\bin\Debug\
                │
                ├── Malie_Script_Tool.exe
                │
                ├── data\system\
                │   ├── exec.msg.txt    ← main dialogue
                │   ├── exec.str.txt    ← character names & UI strings
                │   └── exec.org.dat    ← original script, do not change
                │
                └── .data\system\       ← CREATE MANUALLY (Vertical); already exists (Horizontal)
                    └── exec.dat        ← compile output
```

---

Credits: Tooling by Monaco A. Knox. Reference: [Dies Irae](https://github.com/Monaco-a-Knox/amantesamentes).
