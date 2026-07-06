# Malie / FreeMalie Engine

**Tab Name:** Malie  
**Powered by:** FreeMalie Tools (bundled)  
**File Formats:** `.dat`, `.lib` (archives), `.mgf` (images)

---

## Supported Games

| Game | Notes |
|---|---|
| Dies irae ~Also sprach Zarathustra~ | See also: **Malie (Kajiri)** tab for Kajiri Kamui Kagura |
| Sharin no Kuni, Himawari no Shoujo | `.dat` / `.lib` archives |
| G-Senjou no Maou | `.dat` / `.lib` archives |

---

## Overview

The Malie/FreeMalie engine stores game resources in encrypted `.dat` / `.lib` archives and uses `.mgf` format for images. Scripts inside the archives contain both name (character name) and dialog (dialogue lines) sections that must be patched separately and **in a specific order**.

This tab has **three independent sections**:
- **Archive** — decrypt and re-encrypt `.dat`/`.lib` archives
- **Script Translation** — export and patch Names and Dialog
- **Image** — convert `.mgf` images to/from `.png`

---

## Section A — Archive (Decrypt & Re-encrypt)

### Step 1 — Decrypt Archive

1. Under **ARCHIVE**, click **Browse** next to **INPUT ARCHIVE** and select your `.dat` or `.lib` file.
2. Click **Decrypt Archive**.
3. The decrypted contents are extracted to a folder next to the archive.

### Step 2 — Re-encrypt Archive

1. After making all your script and image edits, click **Re-encrypt Archive**.
2. The tool repacks and re-encrypts the modified files back into a `.dat` or `.lib` archive.
3. Replace the original archive in your game directory with the new file.

---

## Section B — Script Translation (Names & Dialog)

> ⚠️ **CRITICAL ORDER — Patch Names BEFORE Patch Dialog.** Always patch character names first, then dialog. Patching dialog first will cause name/dialog misalignment and corrupt the script output.

### Step 3 — Export Names

1. Under **SCRIPT**, click **Export Names**.
2. A file containing all character name strings is exported for translation.
3. Translate the character names and save.

### Step 4 — Export Dialog

1. Click **Export Dialog**.
2. A file containing all dialogue strings is exported for translation.
3. Translate the dialogue and save.

### Step 5 — Patch Names (FIRST!)

1. Click **Patch Names**.
2. Your translated character names are injected into the script data.

> ⚠️ **Patch Names must happen before Patch Dialog.** The dialog patching step references the name data that was just written. Reversing this order causes corruption.

### Step 6 — Patch Dialog (SECOND!)

1. Click **Patch Dialog**.
2. Your translated dialogue is injected into the script data.

---

## Section C — Image (MGF ↔ PNG)

### Step 7 — MGF → PNG

1. Under **IMAGE**, click **Browse** next to **INPUT .MGF** and select an image file.
2. Click **MGF → PNG**.
3. A `.png` is saved next to the original `.mgf`.
4. Edit the image in your preferred image editor.

### Step 8 — PNG → MGF

1. Click **Browse** next to **MODIFIED .PNG** and select your edited image.
2. Click **PNG → MGF** to convert back.
3. Place the new `.mgf` back into the decrypted archive folder before re-encrypting.

---

## Full Workflow Summary

```
① Decrypt Archive → files extracted to folder

SCRIPT (in this exact order!):
② Export Names → translate → Patch Names  ← FIRST
③ Export Dialog → translate → Patch Dialog ← SECOND

IMAGES (as needed):
④ MGF → PNG → edit → PNG → MGF → place back in folder

FINAL:
⑤ Re-encrypt Archive → new .dat/.lib
⑥ Replace original archive in game directory → test
```

> ⚠️ **Patch Names BEFORE Patch Dialog — every time.** This is the single most critical rule for Malie. Violating the order will silently corrupt the script.
