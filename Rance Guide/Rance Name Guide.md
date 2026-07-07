# How to Change Rance's Name, Rance 10 / Every Rance

---

## Background

After you decode `Rance10EX.ex` using Alice Engine tools and got `Rance10EX.txt`, you will find a table called:

```
table 立ち絵名札マッピング情報 = {
```

This is the **Character Name Plate Mapping Table**. It maps internal character identifiers to their display names in-game.

It looks like this:

```
table 立ち絵名札マッピング情報 = {
	{ indexed string stand, string name },
	{ "アーシー／", "アーシー・ジュリエッタ" },
	{ "アームズ／", "アームズ・アーク" },
	{ "アールコート／", "アールコート・マリウス" },
	{ "アギレダ", "アギレダ・コイサブッシ・ゾンナ・アボナ" },
	{ "アスカ／", "アスカ・カドミュウム" },
	{ "アタゴ", "アタゴ・マカット" },
	{ "アトランタ／", "アトランタ" },
	...
```

---

## The Problem

**Rance is not in this table by default.**

So if you search for `ランス` or `Rance` in this section, you will not find him.

This means you cannot change his displayed name through the normal method, because there is no entry to edit in the first place.

---

## Why You Can Still Add Him

The Alicesoft engine reads this table **dynamically at runtime**. It does not hardcode which characters are in the list. 

This means: **if you add a new entry for Rance, the engine will pick it up and use it.**

This is the flexibility of the Alicesoft engine, you are not breaking anything, you are extending the table with a new valid entry.

---

## How to Add Rance to the Table

### Step 1 — Open the file

Open `Rance10EX.txt` in **Notepad++**.

Make sure encoding is set to **Shift-JIS**:
**Encoding → Character sets → Japanese → Shift-JIS**

### Step 2 — Find the table

Press **Ctrl + F** and search for:

```
立ち絵名札マッピング情報
```

Or you can search for a character you know is in the list, for example:

```
アーシー／
```

You will land somewhere inside the table.

### Step 3 — Add the Rance entry

Scroll through the list and find an appropriate place (alphabetical order is fine but not required). Then **add this line**:

```
	{ "ランス／", "Rance" },
```

Example of what it looks like after adding:

```
table 立ち絵名札マッピング情報 = {
	{ indexed string stand, string name },
	{ "アーシー／", "アーシー・ジュリエッタ" },
	{ "アームズ／", "アームズ・アーク" },
	{ "アールコート／", "アールコート・マリウス" },
	{ "アギレダ", "アギレダ・コイサブッシ・ゾンナ・アボナ" },
	{ "アスカ／", "アスカ・カドミュウム" },
	{ "アタゴ", "アタゴ・マカット" },
	{ "アトランタ／", "アトランタ" },
	{ "ランス／", "Rance" },    <- you add this line
	...
```

To change the name to something else, replace `"Rance"` with your desired name:

```
	{ "ランス／", "Alex" },
```

>  Rules:
> - Do NOT change `"ランス／"` — that is the internal identifier the engine uses to look him up.
> - Only change the second value (the display name).
> - Keep the format exactly: tab, open brace, first string, comma, second string, close brace, comma.

### Step 4 — Save

**Ctrl + S** → save as **Shift-JIS**.

---

## Step 5 — Recompile the EX file

After saving, you need to recompile `Rance10EX.txt` back into the game's binary format using my Alice Engine tools:

Then replace the original `.ex` file in the game directory with the newly compiled one.

---

## Why This Works

The engine does not have a fixed hardcoded list of which characters are in the name plate table. It reads **all entries in the table** at startup and builds an internal lookup map.

So when you add `{ "ランス／", "Rance" }`, the engine adds that mapping to its internal map, and whenever it needs to display Rance's name plate, it finds the entry and shows `"Rance"` (or whatever you put there).

You are not hacking or patching anything. You are using the system exactly as it was designed, just extending the data.

---

## Quick Summary

| Step | What to do |
|---|---|
| 1 | Open `Rance10EX.txt` in Notepad++ with Shift-JIS encoding |
| 2 | Find the `立ち絵名札マッピング情報` table |
| 3 | Add `{ "ランス／", "Rance" },` anywhere inside the table |
| 4 | Change `"Rance"` to the name you want |
| 5 | Save as Shift-JIS |
| 6 | Recompile with `alice ex build` and replace the `.ex` file |

---

## Common Mistakes

| Mistake | Result | Fix |
|---|---|---|
| Changing `"ランス／"` | Engine can't find the entry | Only change the second value |
| Saving as UTF-8 | Japanese text corrupts | Save as Shift-JIS |
| Forgetting to recompile | Changes don't apply | Run `alice ex build` after editing |
| Wrong format (missing comma, wrong brackets) | Compile error | Match the format of other entries exactly |

---

*Based on actual testing with Rance10EX.txt decoded via AliceTools.*
