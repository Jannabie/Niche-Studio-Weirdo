# Fuzz Inc. Engine

**Tab Name:** Fuzz Inc.  
**Powered by:** Fuzz Inc. Tools (bundled)  
**File Formats:** `.epk` (encrypted archives), `decryptKey.bin` (decryption key)

---

## Supported Games

| Game | Platform | Notes |
|---|---|---|
| Fate/stay night Remastered | Steam / PC | Requires `decryptKey.bin` + game `.exe` |

---

## Overview

The Fuzz Inc. engine stores all game resources in encrypted `.epk` archives. Decryption requires both a `decryptKey.bin` file and the game's own `.exe` (which contains part of the decryption key). Once decrypted, scripts are exported to JSON for translation and re-injected back.

This tab also supports **building a Steam-compatible patch** from your translated data.

---

## Step 1 — Load Decryption Keys

1. Click **Browse** next to **DECRYPT KEY FILE** and select `decryptKey.bin` from your game directory.
2. Click **Browse** next to **GAME EXE** and select the game's main `.exe` file.


---

## Step 2 — Decrypt EPK Archive

1. Click **Decrypt EPK**.
2. The tool decrypts the `.epk` archive and extracts its contents to a folder next to the archive.

---

## Step 3 — Export Text → JSON

1. Click **Export → JSON**.
2. A JSON file is generated containing all dialogue and text strings from the decrypted scripts.
3. Open the JSON in a text editor, translate the strings, and save.

---

## Step 4 — Inject Translation → EPK

1. Click **Inject → EPK**.
2. The tool writes your translated strings back into the archive structure.

---

## Step 5 — Build Steam Patch

1. Click **Build Patch**.
2. A Steam-compatible patch file is assembled from your translated `.epk` data.
3. Deploy the patch file according to your release process.


---

## Full Workflow Summary

```
① Browse DECRYPT KEY FILE (decryptKey.bin)
② Browse GAME EXE (game's .exe file)
③ Click Decrypt EPK → decrypted archive contents extracted
④ Click Export → JSON → translate the JSON strings
⑤ Click Inject → EPK → translations written back into archive
⑥ Click Build Patch → Steam-compatible patch file generated
⑦ Deploy patch → test in-game
```

