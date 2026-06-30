# Niche Studio Weirdo

A centralized WPF toolset for reverse-engineering and translating Japanese visual novels, wrapped in a macOS-inspired dark UI. Supports a wide range of VN engines.

![Tools Interface](https://raw.githubusercontent.com/Jannabie/Niche-Studio-Weirdo/refs/heads/main/Tools%20Interface/image.png)

---

## Supported Engines

| Tab Name | Games | Docs |
|---|---|---|
| Abogado (DSK) | Shuumatsu no Sugoshikata | [📖 Guide](docs/engines/abogado-(dsk).md) |
| Abogado (KG) | Shuumatsu no Sugoshikata (images) | [📖 Guide](docs/engines/abogado-(kg).md) |
| Alicesoft | Rance series, Evenicle | [📖 Guide](docs/engines/alicesoft.md) |
| Buriko | Higurashi, Umineko, Sakura no Uta | [📖 Guide](docs/engines/buriko.md) |
| codeX RScript | Various | [📖 Guide](docs/engines/codex-rscript.md) |
| Fuzz Inc. | Fate/stay night Remastered | [📖 Guide](docs/engines/fuzz-inc.md) |
| HuneX (Tsukihime) | Tsukihime Remake | [📖 Guide](docs/engines/hunex-(tsukihime).md) |
| HuneX (Witch on The Holy Night) | Mahoyo Remastered | [📖 Guide](docs/engines/hunex-(witch-on-the-holy-night).md) |
| Leaf | White Album 2 | [📖 Guide](docs/engines/leaf.md) |
| Malie | Sharin no Kuni, G-Senjou, Kami no Rhapsody | [📖 Guide](docs/engines/malie.md) |
| Malie Kajiri | Kajiri Kamui Kagura, Dies irae | [📖 Guide](docs/engines/malie-kajiri.md) |
| Minato (New) | Wagamama High Spec | [📖 Guide](docs/engines/minato-(new).md) |
| Minato (Old) | Majikoi series | [📖 Guide](docs/engines/minato-(old).md) |
| Minori | ef series, eden*, Supipara | [📖 Guide](docs/engines/minori.md) |
| TYPE-MOON | Melty Blood: Type Lumina | [📖 Guide](docs/engines/type-moon.md) |
| YOX | Musicus! | [📖 Guide](docs/engines/yox.md) |

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
