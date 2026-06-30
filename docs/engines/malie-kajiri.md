# Malie Kajiri (KKK)

> Exclusive toolkit for Kajiri Kamui Kagura — compile scripts, pack data, and patch message frames.

**Example games:** Kajiri Kamui Kagura (KKK), Dies irae (Malie engine variant)

---

## Tools Available

### 1. Translation Layout Patch

Installs a base patch that adjusts the text box layout for translated (non-Japanese) text.

| Option | Description |
|---|---|
| **Horizontal ADV** | Standard left-to-right text layout |
| **Vertical ADV** | Vertical text layout (Japanese default) |

**How to use:**
1. Browse → select your **Game Directory** (root folder of the game)
2. Select the appropriate **Layout Mode** radio button
3. Click **Install Base Patch**

> Run this **once** before building any translations.

---

### 2. Build Translation Pipeline (3 Steps)

After installing the base patch, build your translation in order:

#### Step 1 — Wordwrap (Optional)
Runs `wordwrap.py` to automatically wrap long lines to fit the text box.
- Click **Run Wordwrap**
- This processes the script files in your `dependencies/` folder

#### Step 2 — Compile Script
Runs `Malie_Script_Tool.exe` to compile your scripts into `exec.dat`.
- Click **Compile Script**
- Output: `exec.dat` in the game data folder

#### Step 3 — Pack data6.dat
Packs all compiled data into the final `data6.dat` archive.
- Click **Pack data6.dat**
- This runs `dat_pack.py` targeting the local `data/` folder
- Output: `data6.dat` ready to replace in the game directory

---

## Full Workflow

```
Install Base Patch → (Wordwrap) → Compile Script → Pack data6.dat → Replace in game
```
