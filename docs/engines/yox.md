# YOX Engine (Musicus!)

> A 4-step pipeline: `.dat` → `.dec` → JSON (edit) → `.dec` → `.dat`.

**Example games:** Musicus!

---

> ⚠️ **Critical:** This engine uses a strict two-stage pipeline. Complete each step **in order**. If you skip a step or re-run steps out of sequence, `manifest.json` will lose its context and repacking will fail.

---

## Workflow Overview

```
STEP 1          STEP 2          STEP 3          STEP 4
Unpack DAT  →   Export JSON →   Import JSON →   Repack DAT
(.dat→.dec)    (.dec→.json)    (.json→.dec)   (.dec→.dat)
```

---

## Step-by-Step

### Step 1 — Unpack DAT
- Browse → select your `.dat` file from the game
- Click **Unpack DAT**
- Output: a `.dec` (decrypted) file + `manifest.json`

### Step 2 — Export JSON
- The tool uses the `.dec` file from Step 1
- Click **Export JSON**
- Output: a `.json` file containing all translatable text

### Step 3 — Translate & Import JSON
- Open the `.json` and translate the text values
- **Do NOT rename or move `manifest.json`** — it must remain in the same folder
- Click **Import JSON**
- Output: a new `.dec` file with your translations

### Step 4 — Repack DAT
- Click **Repack DAT**
- Output: a new `.dat` file ready for the game
- Replace the original `.dat` in your game folder

---

> **If anything goes wrong:** Start over from Step 1. Do not try to reuse `.dec` files from a different session — `manifest.json` is session-specific.
