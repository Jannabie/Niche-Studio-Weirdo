# Niche Studio — Documentation

> A unified translation toolkit for multiple Japanese visual novel engines, built with WPF/.NET 8.

## Supported Engines

| Tab Name | Games | File Formats |
|---|---|---|
| [Abogado (DSK)](engines/abogado-(dsk).md) | Shuumatsu no Sugoshikata series | `.dsk`, `.pft`, `.scf` |
| [Abogado (KG)](engines/abogado-(kg).md) | Shuumatsu no Sugoshikata (images) | `.kg` |
| [Alicesoft](engines/alicesoft.md) | Rance series, Evenicle | `.ain`, `.afa`, `.ald`, `.cg`, `.ex` |
| [Buriko](engines/buriko.md) | Higurashi, Umineko, Sakura no Uta | `.arc`, `.sc` |
| [codeX RScript](engines/codex-rscript.md) | Various | `.gsc` |
| [Fuzz Inc.](engines/fuzz-inc.md) | Fate/stay night Remastered | `.epk`, `.bin` |
| [HuneX (Tsukihime)](engines/hunex-(tsukihime).md) | Tsukihime Remake | `.mrg` |
| [HuneX (Witch on The Holy Night)](engines/hunex-(witch-on-the-holy-night).md) | Mahoyo Remastered | `.hfa`, `.ctd`, `.cbg`, `.mzp` |
| [Leaf](engines/leaf.md) | White Album 2 | `.pak` (KCAP) |
| [Malie](engines/malie.md) | Sharin no Kuni, G-Senjou, Kami no Rhapsody | `.dat`, `.lib`, `.mgf` |
| [Malie Kajiri](engines/malie-kajiri.md) | Kajiri Kamui Kagura, Dies irae | Custom |
| [Minato (New)](engines/minato-(new).md) | Wagamama High Spec | `.dat` (ACV1) |
| [Minato (Old)](engines/minato-(old).md) | Majikoi series | `.pac`, `.bin` |
| [Minori](engines/minori.md) | ef series, eden*, Supipara | `.paz` |
| [TYPE-MOON](engines/type-moon.md) | Melty Blood: Type Lumina | `.p` |
| [YOX](engines/yox.md) | Musicus! | Custom |

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
