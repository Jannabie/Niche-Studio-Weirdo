# Minato Old Engine (Majikai)

> Unpack `.pac` archives and extract/repack `.bin` script SEG blocks.

**Example games:** Majikoi series and other older Minato Soft titles

---

## Tools Available (2 panels)

### Left Panel: PAC Archive Operations

| Button | Action |
|---|---|
| **Unpack PAC** | Extract all files from a `.pac` archive into a folder |
| **Repack PAC** | Pack a folder back into a `.pac` archive |

**How to use:**
1. Browse → select your `.pac` file
2. Browse → select output folder (for unpack) or source folder (for repack)
3. Click **Unpack PAC** or **Repack PAC**

---

### Right Panel: BIN Script Operations

The `.bin` script files contain dialogue stored as SEG (segment) blocks.

| Button | Action |
|---|---|
| **Extract SEG** | Extract text segments from `.bin` to an editable format |
| **Repack SEG** | Inject translated text back into the `.bin` |

**Repack Mode options:**
- Choose the appropriate mode matching your game version before repacking

**How to use:**

**Step 1 — Extract:**
1. Browse → select your `.bin` script file
2. Click **Extract SEG** → save the output text file

**Step 2 — Translate:**
- Open the extracted file and translate each text entry

**Step 3 — Repack:**
1. Select the **Repack Mode** that matches your game
2. Browse → select the original `.bin` and the translated text file
3. Click **Repack SEG** → save the new `.bin`
4. Repack it back into the `.pac` archive

---

## Full Workflow

```
Unpack .pac → Extract .bin SEG → Translate → Repack .bin → Repack .pac
```
