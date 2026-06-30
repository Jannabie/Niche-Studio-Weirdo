# Abogado DSK — Archive & Script Tools

> Unpack, patch, and repack Abogado SDK `.dsk` archives and `.scf` script files.

**Example games:** Shuumatsu no Sugoshikata series

---

## Tools Available

### 1. DSK Archive (`.dsk` + `.pft`)

The `.dsk` file is the main archive container. It always comes paired with a `.pft` index file.

| Button | Action |
|---|---|
| **Unpack Archive** | Extract all files from `.dsk` into a folder |
| **Repack Archive** | Pack a folder back into a new `.dsk` + `.pft` pair |

**How to use:**
1. Browse → select your `.pft` file (the index)
2. Browse → select your `.dsk` file (the data)
3. Browse → select output folder
4. Click **Unpack Archive** to extract

**To repack:**
1. Browse → select the folder containing your modified files
2. Click **Repack Archive** → choose where to save the new `.dsk` and `.pft`

---

### 2. SCF Script Tools (`.scf`)

Abogado script files contain the dialogue and game logic.

| Button | Action |
|---|---|
| **Parse → JSON** | Extract text from a `.scf` file into a `.json` |
| **Inject → SCF** | Inject translated JSON back into the `.scf` |

**How to use:**

**Step 1 — Parse:**
1. Select your `.scf` script file
2. Click **Parse → JSON** → save the output `.json`

**Step 2 — Translate:**
- Open the `.json` and translate the dialogue values

**Step 3 — Inject:**
1. Select the original `.scf`
2. Select your translated `.json`
3. Click **Inject → SCF** → save the new script
4. Repack it back into the `.dsk` archive

---

## See Also

- [Abogado KG (Image Tools)](abogado-kg.md) — for converting `.kg` image files
