# Rance 10 — MC Name Change Guide
> For beginners. No coding experience needed.

---

## What Are These Files?

When you dump the game scripts using **AliceTools**, you get two main text files:

| File | What's inside |
|---|---|
| `Rance10.txt` | Main game script (dialogue, story, etc.) |
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
2. Go to **File → Open** and select `Rance10EX.txt`
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

## Changing Rance's Name (the MC)

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

## Changing Sill's Name (for reference)

Same process — find:

```
{ "シィル／", "Sill Plain" },
```

And change `"Sill Plain"` to whatever you want.

---

## Other Name Entries Nearby

While you're in that section, you'll also see entries like:

```
{ "ランス／", "Rance" },          <- Normal Rance
{ "ランス２／", "魔王 ランス" },   <- Demon King Rance (alternate form)
```

If you want to rename the Demon King form too, change the second entry's value as well.

---

## After Editing — Rebuild the Game Files

Once you've edited `Rance10EX.txt`, you need to **repack it** back into the game using AliceTools:

```bash
alice ain compile Rance10EX.txt -o System40.ain
```

Then replace the original `System40.ain` in the game directory with your new one.

> The exact AliceTools command may vary depending on your version. Check the AliceTools documentation for the correct flags.

---

## Quick Reference

| What | Where | What to change |
|---|---|---|
| Rance's name | `Rance10EX.txt` line ~209981 | `{ "ランス／", "Rance" }` → change `"Rance"` |
| Sill's name | `Rance10EX.txt` | `{ "シィル／", "Sill Plain" }` → change `"Sill Plain"` |

---

## Common Mistakes

| Mistake | What happens | Fix |
|---|---|---|
| Editing `"ランス／"` | Game breaks / name doesn't show | Only edit the **second** value |
| Saving as UTF-8 | Japanese text corrupts | Save as **Shift-JIS** |
| Wrong file opened | Changes don't appear | Make sure you edited `Rance10EX.txt`, not `Rance10.txt` |
| Forgot to recompile | Changes don't apply in-game | Run `alice ain compile` after editing |

---

*Guide written based on actual file analysis of Rance10EX.txt dumped via AliceTools.*
