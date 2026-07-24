# Niche Studio — Documentation

> A unified translation toolkit for multiple Japanese visual novel engines, built with WPF/.NET 8.

## Supported Engines

| Tab Name | Games | File Formats |
|---|---|---|
| [Abogado (DSK)](engines/abogado-(dsk).md) | Shuumatsu no Sugoshikata | `.dsk`, `.pft`, `.scf` |
| [Abogado (KG)](engines/abogado-(kg).md) | Shuumatsu no Sugoshikata | `.kg` |
| [Alicesoft](engines/alicesoft.md) | Rance series, Evenicle, etc | `.ain`, `.afa`, `.ald`, `.cg`, `.ex` |
| [Buriko](engines/buriko.md) | Subarashiki Hibi, Sakura no Uta, etc| `.arc`, `.sc` |
| [codeX RScript](engines/codex-rscript.md) | Various | `.gsc` |
| [Diesel Engine](engines/diesel-engine.md) | Saya no Uta, Tokyo Necro, DRAMAtical Murder, Muramasa | `.npk`, `.nut` |
| [Fuzz Inc.](engines/fuzz-inc.md) | Fate/stay night Remastered | `.epk`, `.bin` |
| [FVP Engine](engines/fvp.md) | Sakura Moyu, etc | `.bin`, `.hcb`, `.hzc` |
| [HuneX (Tsukihime)](engines/hunex-(tsukihime).md) | Tsukihime Remake | `.mrg` |
| [HuneX (Witch on The Holy Night)](engines/hunex-(witch-on-the-holy-night).md) | Mahoyo Remastered | `.hfa`, `.ctd`, `.cbg`, `.mzp` |
| [Leaf](engines/leaf.md) | White Album 2 | `.pak` (KCAP) |
| [Luca System](engines/luca-system.md) | LOOPERS, Little Busters, etc | `SCRIPT.PAK`, `.CZ0`–`.CZ3` |
| [Malie](engines/malie.md) | Dies Irae, etc | `.dat`, `.lib`, `.mgf` |
| [Malie Kajiri](engines/malie-kajiri.md) | Kajiri Kamui Kagura, etc | Custom |
| [Minato (New)](engines/minato-(new).md) | Waga Himegimi ni Eikan o, etc | `.dat` (ACV1) |
| [Minato (Old)](engines/minato-(old).md) | Majikoi series, etc | `.pac`, `.bin` |
| [Minori](engines/minori.md) | ef series, eden*, Supipara, etc | `.paz` |
| [N2System](engines/n2system.md) | Django, Chaos;Head, Lamento, DRAMAtical Murder, sweet pool | `.npa` |
| [TYPE-MOON](engines/type-moon.md) | Melty Blood 2002 | `.p` |
| [YOX](engines/yox.md) | Musicus! | Custom |
| [YU-RIS](engines/yuris.md) | Maggot Baits, Erewhon, ef series | `.ypf`, `.ybn` |
| [rUGP Engine](engines/rugp.md) | Schwarzesmarken | `.jlx` |
| [Lasengle](engines/lasengle.md) | MELTY BLOOD: TYPE LUMINA | Hook-based |

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
