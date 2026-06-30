# Malie Engine

> Decrypt `.dat`/`.lib` archives, translate scripts, and convert MGF graphics.

**Example games:** Sharin no Kuni, G-Senjou no Maou, Kami no Rhapsody, Hatsuyuki Sakura

---

## Tools Available (3 tabs)

### Tab 1: Archive (`.dat` / `.lib`)

Malie archive files are encrypted and must be decrypted before accessing their contents.

| Button | Action |
|---|---|
| **Decrypt Archive** | Decrypt a `.dat` or `.lib` file |
| **Re-encrypt Archive** | Re-encrypt for use back in the game |

**How to use:**
1. Browse → select your `.dat` or `.lib` archive
2. Click **Decrypt Archive** → save the decrypted output
3. After modifying contents, click **Re-encrypt Archive** to restore encryption

---

### Tab 2: Script Translation

> ⚠️ **Important:** Export **Names FIRST**, then export **Dialog**. They must be patched in separate passes.

| Button | Action |
|---|---|
| **Export Names** | Extract character name strings to a file |
| **Export Dialog** | Extract dialogue text to a file |
| **Patch Names** | Inject translated names back into the scripts |
| **Patch Dialog** | Inject translated dialogue back into the scripts |

**How to use:**

**Step 1 — Select Script Directory:**
- Browse → select the folder containing the decrypted script files

**Step 2 — Export:**
1. Click **Export Names** → save the names file
2. Click **Export Dialog** → save the dialog file

**Step 3 — Translate:**
- Edit both files with your translations

**Step 4 — Patch:**
1. Click **Patch Names** → select your translated names file
2. Click **Patch Dialog** → select your translated dialog file
3. Re-encrypt the archive and place it back in the game folder

---

### Tab 3: Graphics (MGF)

Malie games use `.mgf` (Malie Graphics Format) for images.

| Button | Action |
|---|---|
| **MGF → PNG** | Convert `.mgf` to standard PNG |
| **PNG → MGF** | Convert edited PNG back to `.mgf` |

**How to use:**
1. Browse → select your `.mgf` file
2. Click **MGF → PNG** → save and edit
3. Click **PNG → MGF** when done → replace in the archive
