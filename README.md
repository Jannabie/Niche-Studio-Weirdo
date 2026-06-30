# Niche Studio Weirdo

A centralized WPF toolset for reverse-engineering and translating Japanese visual novels, wrapped in a macOS-inspired dark UI. Supports a wide range of VN engines.

![Tools Interface](https://raw.githubusercontent.com/Jannabie/Niche-Studio-Weirdo/refs/heads/main/Tools%20Interface/image.png)

---

## Supported Engines

| Engine | Games | Docs |
|---|---|---|
| Alicesoft | Rance series, Evenicle | [📖 Guide](docs/engines/alicesoft.md) |
| Abogado DSK | Shuumatsu no Sugoshikata | [📖 Guide](docs/engines/abogado-dsk.md) |
| Abogado KG | Shuumatsu no Sugoshikata (images) | [📖 Guide](docs/engines/abogado-kg.md) |
| Buriko (BGI) | Higurashi, Umineko, Sakura no Uta | [📖 Guide](docs/engines/buriko.md) |
| codeX RScript | Various | [📖 Guide](docs/engines/codex-rscript.md) |
| Fuzz Inc. | Fate/stay night Remastered | [📖 Guide](docs/engines/fuzz-inc.md) |
| HuneX (Tsukihime) | Tsukihime Remake | [📖 Guide](docs/engines/hunex.md) |
| HuneX (Mahoyo) | Witch on the Holy Night | [📖 Guide](docs/engines/hunex-mahoyo.md) |
| Malie | Sharin no Kuni, G-Senjou, Kami no Rhapsody | [📖 Guide](docs/engines/malie.md) |
| Malie Kajiri (KKK) | Kajiri Kamui Kagura, Dies irae | [📖 Guide](docs/engines/kkk.md) |
| Minato Old (Majikai) | Majikoi series | [📖 Guide](docs/engines/majikai.md) |
| Minato New (Waga Hime) | Wagamama High Spec | [📖 Guide](docs/engines/waga-hime.md) |
| Minori | ef series, eden*, Supipara | [📖 Guide](docs/engines/minori.md) |
| TYPE-MOON (Melty Blood) | Melty Blood: Type Lumina | [📖 Guide](docs/engines/melty-blood.md) |
| YOX (Musicus!) | Musicus! | [📖 Guide](docs/engines/musicus.md) |
| Leaf (WA2 Archive) | White Album 2 | [📖 Guide](docs/engines/wa2-archive.md) |

📚 **[Full Documentation Index →](docs/README.md)**

---

## General Translation Workflow

For almost every engine, the workflow follows the same pattern:

```
1. EXTRACT / DUMP   →   Get readable text out of the game files
2. TRANSLATE        →   Edit the text with your translations  
3. INJECT / BUILD   →   Put the translated text back into game files
4. TEST             →   Replace the original file in-game and test
```

---

## UI

Emulates a macOS terminal with dark mode, tab navigation, and `SF Mono Medium` font. Install the font from the `Font/` directory for the best experience.

---

## Build

Requires .NET 8.0 SDK and Windows (WPF). Run `dotnet publish` or open in Visual Studio 2022. Pre-compiled executables are distributed separately via Releases.

```
dotnet publish NicheStudioWeirdo.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
