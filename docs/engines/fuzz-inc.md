# Fuzz Inc. (Fate/stay night Remastered)

> Decrypt EPK files, translate to JSON, and build patches for Steam.

**Example games:** Fate/stay night [Remastered]

---

## Tools Available

### Crypto Keys Setup

Before doing anything, you need to provide the encryption keys from the game.

| Field | Description |
|---|---|
| `decryptKey.bin` | The decryption key file from the game install |
| `main.exe` | The game's main executable (used for key extraction) |

**How to set up:**
1. Browse → locate `decryptKey.bin` in your game folder
2. Browse → locate `main.exe` in your game folder
3. These are required for all operations

---

### EPK Tools

Fate/stay night Remastered uses `.epk` archives.

| Button | Action |
|---|---|
| **Decrypt EPK** | Decrypt an `.epk` file for editing |
| **Export → JSON** | Extract dialogue into a `.json` file |
| **Inject → EPK** | Inject translated JSON back |
| **Build Patch** | Create a Steam-compatible patch that bypasses `.bin` |

**How to use:**

**Step 1 — Decrypt:**
1. Browse → select your `.epk` file
2. Click **Decrypt EPK** → save the decrypted file

**Step 2 — Export:**
1. Select the decrypted file
2. Click **Export → JSON** → save the output

**Step 3 — Translate:**
- Edit the JSON file with your translations

**Step 4 — Inject & Patch:**
1. Click **Inject → EPK** to create the modified archive
2. Click **Build Patch** to generate a Steam-compatible patch file
3. Place the patch file in the game directory as instructed

> **Steam Note:** This tool includes a patch builder specifically to work around Steam's content verification on `.bin` files.
