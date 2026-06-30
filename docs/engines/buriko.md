# Buriko (BGI / Ethornell)

> Parse, translate, and rebuild Ethornell `.sc` script files.

**Example games:** Higurashi When They Cry, Umineko When They Cry, Sukimazakura to Uso no Machi

---

## Tools Available

### 1. Archive Tools (BGI `.arc`)

Used to extract and repack BGI `.arc` archives.

| Button | Action |
|---|---|
| **Extract .arc → Folder** | Unpack all files from the archive into a folder |
| **Repack Folder → .arc** | Pack a folder back into a `.arc` archive |

**How to use:**
1. Browse Arc → select your `.arc` file
2. Browse Dir → select the destination folder (for extract) or source folder (for repack)
3. Click **Extract** or **Repack**

---

### 2. Script Translation Tools

Used to extract and inject text from compiled Ethornell `.sc` script files.

| Button | Action |
|---|---|
| **Parse Script → JSON** | Extract all dialogue from a `.sc` file into a `.json` |
| **Inject JSON → Script** | Inject translated JSON back into the script |

**How to use:**

**Step 1 — Parse:**
1. Browse (top) → select your script file (e.g. `skp001`)
2. Click **Parse Script → JSON** → save the output `.json` file

**Step 2 — Translate:**
- Open the `.json` in any text editor
- Each entry contains the original Japanese text — replace the value with your translation
- **Do not change the keys (IDs)**

**Step 3 — Inject:**
1. Browse (top) → select the **original** script file
2. Browse (bottom) → select your translated `.json` file
3. Click **Inject JSON → Script** → save the new patched script
4. Put it back in the game folder (inside the `.arc` if needed)
