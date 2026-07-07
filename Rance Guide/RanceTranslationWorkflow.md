# Rance 10 — MC Name Change Guide
> For beginners. No coding experience needed.

---

## What Are These Files?

When you dump the game scripts using **AliceTools**, you get two main text files:

| File | What's inside |
|---|---|
| `Rance10.txt` | Main game script (dialogue, story, variables, etc.) |
| `Rance10EX.txt` | Extra data — character cards, name tables, BGM info, etc. |

Both files are encoded in **Shift-JIS (CP932)**, which means you need to open them with the correct encoding or Japanese characters will look broken.

---

## Tools You Need

- **Notepad++** (free) → https://notepad-plus-plus.org
  - Make sure to set encoding to **Shift-JIS** when opening the file.
- OR any text editor that supports Shift-JIS / Japanese encoding.

---

## How to Open the File Correctly

1. Open **Notepad++**
2. Go to **File → Open** and select the file you want to edit
3. If Japanese characters look broken (like `????`), go to:
   **Encoding → Character sets → Japanese → Shift-JIS**
4. The Japanese text should now display correctly.

---

## How the Name Table Works

Deep inside `Rance10EX.txt`, there is a **name substitution table**. It looks like this:

```
{ "シィル／", "Sill Plain" },
{ "ランス／", "Rance" },
```

This table tells the game: *"When you see this internal code, display this name instead."*

- The **first value** (Japanese with `／` at the end) = the internal identifier. **DO NOT touch this.**
- The **second value** (the name in quotes) = the display name shown in-game. **This is what you change.**

---

## Method 1 — Change Name via Rance10EX.txt (Preferred)

### Step 1 — Find the entry

In Notepad++, press **Ctrl + F** (Find), then search for:

```
"ランス／", "Rance"
```

> If the search finds nothing, make sure the file encoding is set to Shift-JIS (see above).

You should land on a line that looks like this:

```
	{ "ランス／", "Rance" },
```

### Step 2 — Change the name

Replace only the second value. For example, to rename Rance to **"Alex"**:

**Before:**
```
	{ "ランス／", "Rance" },
```

**After:**
```
	{ "ランス／", "Alex" },
```

> ⚠️ **Important rules:**
> - Keep the curly braces `{ }`, the comma `,`, and the quotes `"` exactly as they are.
> - Only change the text between the **second pair** of quotes.
> - Do NOT touch `"ランス／"` — that is the internal game code.

### Step 3 — Save the file

Press **Ctrl + S** to save.

> If Notepad++ asks about encoding when saving, choose **Shift-JIS / ANSI**.

---

## Method 2 — What If the Entry is NOT in Rance10EX.txt?

This can happen if:
- You are working on a **different Alicesoft game** (not Rance 10)
- You dumped from a **different version** of the game
- The name is **hardcoded** directly in the main script instead of the name table

In that case, the name is stored inside **`Rance10.txt`** as a string variable assignment.

### What it looks like in Rance10.txt

Instead of a clean name table, it will look more like this inside the raw script:

```
string ランス.名前 = "Rance";
```
or
```
string ランス.フルネーム = "Rance";
```

These are **variable declarations** inside the AIN bytecode. The game reads the variable value directly at runtime.

### Step 1 — Search in Rance10.txt

> ⚠️ WARNING: `Rance10.txt` is a very large file (~24 MB). Notepad++ may be slow.
> Use **PowerShell** to search first, then go to the exact line number in Notepad++.

Open **PowerShell** (press `Win + R`, type `powershell`, press Enter) and run:

```powershell
$enc = [System.Text.Encoding]::GetEncoding(932)
$lines = [System.IO.File]::ReadAllLines("C:\path\to\Rance10.txt", $enc)
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '"Rance"' -or $lines[$i] -match 'RANCE') {
        Write-Host "Line $($i+1): $($lines[$i])"
    }
}
```

Replace `C:\path\to\Rance10.txt` with the actual path on your machine.

This will print all line numbers containing `"Rance"` or `RANCE` — fast, even on large files.

### Step 2 — Go to that line in Notepad++

Once you know the line number, open Notepad++ and press:

**Ctrl + G** → type the line number → press Enter

You will jump directly to that line.

### Step 3 — Edit the value

Same rule as before — only change the text inside the **second pair** of quotes.

**Before:**
```
string ランス.名前 = "Rance";
```

**After:**
```
string ランス.名前 = "Alex";
```

### Step 4 — Save

**Ctrl + S**, save as **Shift-JIS**.

---

## Other Name Entries to Know

While you're searching, you might also find:

```
{ "ランス／", "Rance" },          <- Normal Rance (in Rance10EX)
{ "ランス２／", "魔王 ランス" },   <- Demon King Rance (alternate form)
```

If you want to rename the Demon King form too, change its second value as well.

Also for Sill:

```
{ "シィル／", "Sill Plain" },
```

---

## After Editing — Rebuild the Game Files

Once you've edited any `.txt` file, you need to **repack it** back into the game using AliceTools.

For `Rance10EX.txt`:
```bash
alice ex build Rance10EX.txt -o ExHihi.ex
```

For `Rance10.txt` (the main AIN script):
```bash
alice ain compile Rance10.txt -o System40.ain
```

Then replace the original files in your game directory with the new compiled ones.

> Check the AliceTools `README-ain.md` and `README-ex.md` for exact flags and options.

---

## Quick Reference

| What | File to edit | What to search | What to change |
|---|---|---|---|
| Rance's name (preferred) | `Rance10EX.txt` | `"ランス／", "Rance"` | Change `"Rance"` |
| Rance's name (fallback) | `Rance10.txt` | `"Rance"` | Change `"Rance"` |
| Sill's name | `Rance10EX.txt` | `"シィル／", "Sill Plain"` | Change `"Sill Plain"` |

---

## Common Mistakes

| Mistake | What happens | Fix |
|---|---|---|
| Editing `"ランス／"` | Game breaks / name doesn't show | Only edit the **second** value |
| Saving as UTF-8 | Japanese text corrupts | Save as **Shift-JIS** |
| Editing the wrong file | Changes don't appear | Try `Rance10EX.txt` first, then `Rance10.txt` |
| Forgot to recompile | Changes don't apply in-game | Run the correct `alice` compile command |
| Notepad++ too slow on large file | Editor hangs | Use PowerShell to find the line number first |

---

*Guide written based on actual file analysis of Rance10EX.txt and Rance10.txt dumped via AliceTools.*
