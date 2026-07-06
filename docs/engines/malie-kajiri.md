# Malie Engine (Kajiri Kamui Kagura / Dies irae Variant)

**Tab Name:** Malie (Kajiri)  
**Powered by:** FreeMalie Tools (bundled)  
**File Formats:** `.dat`, `.lib` (archives), `.mgf` (images)

---

## Supported Games

| Game | Notes |
|---|---|
| Kajiri Kamui Kagura | Malie engine variant — use this tab specifically |
| Dies irae ~Also sprach Zarathustra~ | Use the **Malie** tab for the standard Dies irae version |

---

## Overview

This tab is a variant of the standard **Malie** engine tab, specifically tuned for **Kajiri Kamui Kagura** and related titles in the Light/Dies irae family. The archive format and script structure are handled the same way as the base Malie engine.

>  **Refer to the [Malie](malie.md) guide for the full detailed workflow.** The steps are identical — Decrypt Archive, Export/Patch Names (first!), Export/Patch Dialog (second!), MGF ↔ PNG, Re-encrypt Archive. This tab simply targets the correct game-specific offsets and keys for Kajiri Kamui Kagura.

---

## Step 1 — Decrypt Archive

1. Click **Browse** next to **INPUT ARCHIVE** and select your `.dat` or `.lib` file from the Kajiri Kamui Kagura game directory.
2. Click **Decrypt Archive** to extract the contents.

---

## Step 2 — Script Translation

>  **CRITICAL ORDER: Patch Names BEFORE Patch Dialog.** This rule applies here exactly as it does in the base Malie tab. Reversing the order will corrupt the script.

1. **Export Names** → translate character names → **Patch Names** ← FIRST
2. **Export Dialog** → translate dialogue → **Patch Dialog** ← SECOND

---

## Step 3 — Image Conversion (MGF ↔ PNG)

1. Use **MGF → PNG** to convert images for editing.
2. After editing, use **PNG → MGF** to convert back.
3. Place modified `.mgf` files back into the extracted folder before re-encrypting.

---

## Step 4 — Re-encrypt Archive

1. Click **Re-encrypt Archive** to pack all modified files back.
2. Replace the original archive in your game directory and test.

---

## Full Workflow Summary

```
① Decrypt Archive → contents extracted

SCRIPT (in this exact order!):
② Export Names → translate → Patch Names  ← FIRST
③ Export Dialog → translate → Patch Dialog ← SECOND

IMAGES (as needed):
④ MGF → PNG → edit → PNG → MGF → place back in folder

FINAL:
⑤ Re-encrypt Archive → new .dat/.lib
⑥ Replace original archive in game directory → test
```


