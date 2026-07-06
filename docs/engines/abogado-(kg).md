# Abogado SDK (KG Image)

**Tab Name:** Abogado (KG)  
**Powered by:** Abogado SDK Tools (bundled)  
**File Formats:** `.kg` (proprietary image format)

---

## Supported Games

| Game | Notes |
|---|---|
| Shuumatsu no Sugoshikata | Image resources only — use **Abogado (DSK)** tab for archive/script |

---

## Overview

The Abogado SDK engine uses a proprietary `.kg` image format for its CG and sprite resources. This tab provides a **one-way conversion** from `.kg` to standard `.png` for editing or translation purposes (e.g., image-based text).

>  This is a **one-way conversion** — there is no `.kg` repacking step. Once images are converted to `.png`, you work with the `.png` files directly (e.g., via a patch or loose-file replacement mechanism).

---

## Step 1 — Select Input Folder

1. Click **Browse** next to **INPUT FOLDER** and select the folder containing your `.kg` image files.

>  **The input folder must contain `.kg` files directly.** Subfolders are not recursively searched, so make sure you point to the correct directory.

---

## Step 2 — Select Output Folder

1. Click **Browse** next to **OUTPUT FOLDER** and select an empty folder where the converted `.png` files will be saved.

---

## Step 3 — Convert KG → PNG

1. Click **Convert KG → PNG**.
2. The tool processes every `.kg` file in the input folder and saves a corresponding `.png` file to the output folder.
3. Open the converted images in Photoshop, GIMP, or any image editor for your translation work.

---

## Full Workflow Summary

```
① Browse INPUT FOLDER (folder containing .kg files)
② Browse OUTPUT FOLDER (empty destination folder)
③ Click Convert KG → PNG
④ Edit the resulting .png files in your image editor
⑤ Use the patched .png files in your patch/release build
```

