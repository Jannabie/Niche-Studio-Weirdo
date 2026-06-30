# BGI Translator

A translation editor application for visual novels built on the **Ethornell / BGI (Buriko General Interpreter)** engine.
Tested on: **Sakura no Uta -Haru no Yuki-** (さくらのうた -桜の詩-)

---

## What Is This?

BGI Translator is a Windows application with a graphical interface that simplifies the process of translating script files (`.sc`) from visual novel games built on the Ethornell engine. This application is built on top of the `EthornellEditor.dll` library by **arcusmaximus**, with additional features not found in EEGUI (arcusmaximus's original editor) such as text search, line filtering, batch glossary, progress bar, TSV export/import, and a dark theme.

| Feature | EEGUI | BGI Translator |
|---|---|---|
| Two-column JP ↔ ID editor | ✗ | ✓ |
| Real-time text search | ✗ | ✓ |
| Untranslated line filter | ✗ | ✓ |
| Automatic tag detection and insertion | ✗ | ✓ |
| Progress bar | ✗ | ✓ |
| TSV export / import | ✗ | ✓ |
| Automatic batch glossary | ✗ | ✓ |
| Drag & drop file support | ✗ | ✓ |
| Dark theme | ✗ | ✓ |

---

## How to Use (Regular Users)

If you just want to use the application directly without building it from source, follow the steps below.

### Step 1 — Download and Setup

Download the **[latest version from the Releases page](https://github.com/Jannabie/BGI-Translator/releases)**. Extract it into a folder, then make sure that folder contains the following files before running it:

| File | Description |
|---|---|
| `BGITranslator.exe` | Main application (downloaded file) |
| `EthornellEditor.dll` | **Required** — download from [arcusmaximus's repo](https://github.com/arcusmaximus/EthornellTools), place it in the same folder |
| `CSystemArc.exe` | For extracting and repacking `.arc` archives (from the same repo) |
| `BgiDisassembler.exe` | For disassembling `.sc` script files — optional |
| `BgiImageEncoder.exe` | For image conversion — optional |

`EthornellEditor.dll` **must be in the same folder** as `BGITranslator.exe`. Without this file, the application cannot open any script files.

### Step 2 — Extract Script Files from the Game Archive

Game script files are stored inside `.arc` archives. Extract their contents using `CSystemArc.exe`:

```bash
CSystemArc.exe extract gamename.arc output_folder\
```

The result is a folder containing `.sc` (dialogue script) files along with the game's other assets.

### Step 3 — Translate with BGI Translator

Run `BGITranslator.exe`, then open the `.sc` file you want to translate via **File → Open Script**, or simply drag & drop the file directly into the application window.

The original Japanese text will appear in the left column, and the right column is where you type the translation. Several features can be used to speed up the work:

**Search** — Type a keyword in the 🔍 box to search the text, navigate between results with `F3` and `Shift+F3`.

**Filter** — Select "Untranslated" mode to show only lines whose translation column is still empty, so you don't have to scroll manually to find unfinished parts.

**Automatic tags** — Special tags such as `\n` (line break) and `@name` (character name) are detected automatically and can be inserted with a single click without typing them manually.

**Batch glossary** — Open the **Tools → Auto Glossary** menu, enter Japanese–English term pairs, then click Apply. All occurrences of those terms throughout the file will be replaced at once.

**TSV export/import** — Translations can be exported to a `.tsv` file for backup or to be worked on collaboratively with others, then imported back once finished via the **File → Export/Import TSV** menu.

Once finished, save with **File → Save**.

### Step 4 — Repack into the Archive

After all scripts have been translated, repack the folder contents back into a `.arc` archive:

```bash
CSystemArc.exe pack output_folder\ gamename_patched.arc
```

Replace the original `.arc` file in the game's installation folder with the patched one, then run the game.

---

## Keyboard Shortcuts

| Key | Action |
|---|---|
| `Ctrl+O` | Open script file |
| `Ctrl+S` | Save |
| `Ctrl+F` | Focus the search box |
| `F3` | Jump to the next search result |
| `Shift+F3` | Jump to the previous search result |
| `Ctrl+G` | Go to a specific line number |
| `Enter` | Enter edit mode for the translation cell |
| `Tab` | Confirm and move to the next line |
| `Esc` | Clear search |

---

## TSV File Format

The exported TSV file uses UTF-8 encoding with tab separators, formatted as follows:

```
# BGI Translator Export | 2025-01-01 12:00
# Index	Original	Translation
0	桜の森で	In the sakura forest
1	世界が鳴った。	The world resonated.
```

This format is compatible and can be opened directly in spreadsheet editors such as LibreOffice Calc or Google Sheets for team collaboration.

---

## Building from Source (For Developers)

If you want to compile it yourself from the source code, here's how.

**Requirements:** Windows 10/11 (64-bit), [.NET 10 SDK](https://dotnet.microsoft.com/download), and `EthornellEditor.dll` placed in the same folder as the `.csproj` file.

```bash
git clone https://github.com/Jannabie/BGI-Translator.git
cd BGI-Translator
dotnet build -c Release
```

The build output will be located at `bin\Release\net10.0-windows\BGITranslator.exe`.

---

## Credits

All capability to read and write the BGI script format comes from the **`EthornellEditor.dll`** library by [arcusmaximus](https://github.com/arcusmaximus). Supporting tools such as `CSystemArc.exe`, `BgiDisassembler.exe`, and `BgiImageEncoder.exe` also come from the same repository. BGI Translator is built on top of these as a more complete interface layer.

---

## Disclaimer

This tool is created for educational and personal translation purposes. Users are fully responsible for ensuring their use complies with the copyright rules and Terms of Service of the relevant game.
