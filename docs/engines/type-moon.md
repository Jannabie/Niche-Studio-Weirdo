# TYPE-MOON Engine (Melty Blood)

> Unpack/repack `.p` archives and edit `.TXT` scripts with Fullwidth (Zenkaku) constraint.

**Example games:** Melty Blood: Type Lumina, and other TYPE-MOON titles using the `.p` archive format

---

> 🚨 **CRITICAL:** Latin alphabets **MUST** be typed in **Fullwidth (Zenkaku)** form.  
> Using half-width ASCII (normal Latin letters) **will corrupt game text**.  
> Use the built-in Fullwidth Converter below before pasting into scripts.

---

## Tools Available

### 1. Unpack `.p` Archive

| Button | Action |
|---|---|
| **Unpack .p** | Extract all files from a `.p` archive into a folder |

**How to use:**
1. Browse File → select your `.p` archive
2. Click **Unpack .p** → files are extracted to a folder next to the archive

---

### 2. Repack `.p` Archive

| Button | Action |
|---|---|
| **Repack .p** | Pack a folder back into a `.p` archive |

**How to use:**
1. Browse Folder → select the folder containing your modified files
2. Click **Repack .p** → save the output `.p` file
3. Replace the original in the game directory

---

### 3. Fullwidth (Zenkaku) Text Converter

This tool converts normal half-width Latin text into Fullwidth characters that the game engine can safely display.

| Field | Description |
|---|---|
| **Input** | Type or paste your normal (half-width) Latin text here |
| **Output** | The converted Fullwidth text — safe to paste into scripts |

**How to use:**
1. Type your translated text in the **Input** field
2. The **Output** field updates automatically with the Fullwidth version
3. Click **Copy Fullwidth Text** to copy it to clipboard
4. Paste into your script file

**Example:**
| Half-width (WRONG ❌) | Fullwidth (CORRECT ✅) |
|---|---|
| `Shiki Tohno` | `Ｓｈｉｋｉ　Ｔｏｈｎｏ` |
| `Arcueid` | `Ａｒｃｕｅｉｄ` |

---

## Full Workflow

```
Unpack .p → Edit .TXT scripts (use Fullwidth converter!) → Repack .p → Replace in game
```
