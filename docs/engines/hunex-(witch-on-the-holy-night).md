# HuneX (Witch on the Holy Night Remastered)

> Tools for Mahoyo Remastered — extract/repack `.hfa` archives and convert script/image files.

**Example games:** Witch on the Holy Night (Mahoyo) Remastered

---

## Tools Available

### 1. HFA Archive Tool (`.hfa`)

The `.hfa` format is the main archive container for Mahoyo Remastered.

| Button | Action |
|---|---|
| **List HFA** | Show all files inside the `.hfa` archive |
| **Unpack HFA** | Extract all contents to a folder |
| **Repack HFA** | Pack a folder back into a `.hfa` archive |

**How to use:**
1. Browse (top) → select your `.hfa` file
2. Browse (bottom) → select a folder (destination for unpack, or source for repack)
3. Click **List HFA** to preview contents, **Unpack HFA** to extract, or **Repack HFA** to pack

---

### 2. CTD / CBG / MZP File Tools

Single-file converters for the individual file types found inside `.hfa` archives.

| File Type | Description |
|---|---|
| `.ctd` | Script/text data files |
| `.cbg` | Compressed background/image files |
| `.mzp` | Compressed sprite/image files |

| Button | Action |
|---|---|
| **Convert / Extract** | Decode the file to an editable format (TXT or PNG) |
| **Repack / Inject** | Re-encode the edited file back to the original format |

**How to use:**
1. Browse → select your `.ctd`, `.cbg`, or `.mzp` file
2. Click **Extract** → save the output (text or image)
3. Edit the extracted file
4. Click **Repack** → select the edited file and save the new output
5. Put the repacked file back into the `.hfa` archive and repack

---

## General Workflow

```
Unpack .hfa → Extract .ctd/.cbg/.mzp → Edit → Repack files → Repack .hfa
```
