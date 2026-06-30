# Niche Studio — Documentation

> A unified translation toolkit for multiple Japanese visual novel engines, built with WPF/.NET 8.

## Supported Engines

| Engine Tab | Games | File Formats |
|---|---|---|
| [Alicesoft](engines/alicesoft.md) | Rance series, etc. | `.ain`, `.afa`, `.ald`, `.cg`, `.ex` |
| [Abogado (DSK)](engines/abogado-dsk.md) | Shuumatsu no Sugoshikata series | `.dsk`, `.pft`, `.scf`, `.kg` |
| [Abogado (KG)](engines/abogado-kg.md) | Shuumatsu no Sugoshikata (images) | `.kg` |
| [Buriko (BGI)](engines/buriko.md) | Higurashi, Umineko, etc. | `.arc`, `.sc` |
| [codeX RScript](engines/codex-rscript.md) | Various | `.rs` |
| [Fuzz Inc.](engines/fuzz-inc.md) | Fate/stay night Remastered | `.epk`, `.bin` |
| [HuneX (Tsukihime)](engines/hunex.md) | Tsukihime Remake | `.mrg` |
| [HuneX (Witch on the H.)](engines/hunex-mahoyo.md) | Witch on the Holy Night | Custom |
| [Malie](engines/malie.md) | Sharin no Kuni, G-Senjou, etc. | `.dat`, `.lib`, `.mgf` |
| [Minori](engines/minori.md) | ef series, eden*, etc. | `.paz` |
| [Musicus](engines/musicus.md) | Musicus! | Custom |
| [WA2 Archive](engines/wa2-archive.md) | White Album 2 | Custom |
| [Waga Hime](engines/waga-hime.md) | Waga Hime | Custom |
| [Melty Blood](engines/melty-blood.md) | Melty Blood | Custom |
| [KKK](engines/kkk.md) | Various | Custom |
| [Majikai](engines/majikai.md) | Various | Custom |

## General Workflow

For almost every engine, the translation workflow follows the same pattern:

```
1. EXTRACT / DUMP   →   Get readable text out of the game files
2. TRANSLATE        →   Edit the text file with your translations
3. INJECT / BUILD   →   Put the translated text back into game files
4. TEST             →   Replace the original file in the game folder and test
```

## Requirements

- Windows 10/11 (64-bit)
- The game files you want to translate
- Optional: Notepad++ for editing large text dumps

## Quick Start

1. Download the latest release of `NicheStudioWeirdo.exe`
2. Launch it — no installation required
3. Select the engine tab matching your target game
4. Follow the specific guide for that engine (linked in the table above)
