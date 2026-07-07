# Rance X Script & Translation Guide

| Preview |
|:---:|
| ![Proof of Translation](https://i.imgur.com/4gXE3L2.jpeg) |
| Translation successfully read by the game after repacking |
---

## 1. Modifying Character Titles/Nicknames (Yellow Text)

In the game, characters often have a title or nickname displayed in yellow text right above their name when they speak (e.g., *Demon King* Rance). 

These titles are stored inside **`Rance10.txt`**.

### How to change it:
Search for the variable `; Ｔ肩書き` (T Katagaki) inside the script. You will find lines of code that look like this:

```
;s[10649] = "アルカネーゼ"
```

Simply replace the text inside the quotation marks with your desired translation:

```
;s[10649] = "Porn Hub King"
```
Once you recompile the script, the yellow text above the character's name will be updated in-game.

---

## 2. Modifying Quest Titles

The titles of the quests that appear in the game's menus are NOT stored in `Rance10.txt`. Instead, they are located in **`Rance10EX.txt`**.

### How to change it:
1. Open `Rance10EX.txt` (Make sure your editor's encoding is set to **Shift-JIS**).
2. Search for the table named `table クエスト情報` (Quest Information Table).
3. The table structure looks like this:

```
table クエスト情報 = {
	{ indexed int Id, string 識別名, int 種別, string クエスト名, string 説明１, string 説明２, string 説明３, string 説明４, int 地域 = 0, int リザルト有無 = 0, int 有利所属 = 0, int 有利属性１ = 0, int 有利属性２ = 0, string 選択画像 = "", int クエストアウト可能 = 0 },
	{ 2, "ホルスの宇宙戦艦", 10, "Reruntuhan Kapal Perang Raksasa", "", "", "", "", 0, 0, 0, 1, 1, "", 0 },
```

4. Focus on the **4th element** in the data row (in the example above, it has already been changed to `"Reruntuhan Kapal Perang Raksasa"`).
5. Replace this string with the translated quest title you want. Leave the rest of the line (commas, numbers, and empty strings) exactly as they are.

---
## 3. Safely Replacing Kagikakko 「 」 with Double Quotes " "

This is a crucial trick if you want to replace the Japanese brackets `「` and `」` with English double quotes `" "` without triggering an `Unterminated string literal` compile error.

In the `Rance10.txt` script, a character's dialogue is often split across two or more lines of code.

### The Correct Format:

If you are splitting a single quoted sentence across two lines, the quotation marks **must** be formatted exactly like this:

```
;m[128991] = "\"Apa-apaan, sih?"
;m[128992] = "Setelah kita masuk ke dalam kapal perang raksasa ini...\""
```

### Why Does This Work? (The Logic Behind It)

- **First Line:** `";m[128991] = "\"Apa-apaan, sih?"`
  - `"` (first one) → Script string opener (required by the engine).
  - `\"` → Produces a literal `" ` character in the game to open the dialogue.
  - `Apa-apaan, sih?` → The dialogue text.
  - `"` (last one) → Script string closer (no backslash, this safely closes the script string for this line).
  - *Result in-game: `"Apa-apaan, sih?`*

- **Second Line:** `";m[128992] = "Setelah kita masuk ke dalam kapal perang raksasa ini...\""`
  - `"` (first one) → Script string opener.
  - `Setelah kita masuk...` → The continuation of the dialogue text.
  - `\"` → Produces a literal `"` character in the game to close the dialogue.
  - `"` (last one) → Script string closer.
  - *Result in-game: `Setelah kita masuk ke dalam kapal perang raksasa ini..."`*

By cross-formatting it this way, you successfully render an opening quote on the first line and a closing quote on the second line **without breaking AliceTools' code structure**. 

---
*This guide is based on direct observation of the `Rance10.txt` and `Rance10EX.txt` file structures dumped via AliceTools.*
