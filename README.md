

# Niche Studio Weirdo
A centralized WPF toolset for reverse-engineering and translating Japanese visual novels, wrapped in a macOS-inspired dark UI. Supports a wide range of VN engines.

![Tools Interface](https://raw.githubusercontent.com/Jannabie/Niche-Studio-Weirdo/refs/heads/main/Tools%20Interface/image.png)

</div>

---
## Supported Engines
> **Update:** Diesel, NS2System, Luca System, YU-RIS, **FVP Engine**.

| Tab Name | Games | Docs |
|---|---|---|
| Abogado (DSK) | Shuumatsu no Sugoshikata | [ Guide](docs/engines/abogado-(dsk).md) |
| Abogado (KG) | Shuumatsu no Sugoshikata | [ Guide](docs/engines/abogado-(kg).md) |
| Alicesoft | Rance series, Evenicle, etc | [ Guide](docs/engines/alicesoft.md) |
| Buriko | Sakura no Uta, Subarashiki Hibi, etc | [ Guide](docs/engines/buriko.md) |
| codeX RScript | Various | [ Guide](docs/engines/codex-rscript.md) |
| Diesel Engine | Full Metal Daemon Muramasa, etc | [ Guide](docs/engines/diesel-engine.md) |
| Fuzz Inc. | Fate/stay night Remastered | [Guide](docs/engines/fuzz-inc.md) |
| FVP Engine | Sakura Moyu, etc | [ Guide](docs/engines/fvp.md) |
| HuneX (Tsukihime) | Tsukihime Remake | [ Guide](docs/engines/hunex-(tsukihime).md) |
| HuneX (Witch on The Holy Night) | Mahoyo Remastered | [ Guide](docs/engines/hunex-(witch-on-the-holy-night).md) |
| Leaf | White Album 2, etc | [ Guide](docs/engines/leaf.md) |
| Luca System | LOOPERS, Little Busters, etc | [ Guide](docs/engines/luca-system.md) |
| Malie |  Dies irae, etc | [Guide](docs/engines/malie.md) |
| Malie Kajiri | Kajiri Kamui Kagura | [ Guide](docs/engines/malie-kajiri.md) |
| Minato (New) | Waga Himegimi ni Eikan o, etc | [ Guide](docs/engines/minato-(new).md) |
| Minato (Old) | Majikoi series, etc | [ Guide](docs/engines/minato-(old).md) |
| Minori | ef series, eden*, etc | [ Guide](docs/engines/minori.md) |
| N2System | Django, Saya no Uta, etc | [ Guide](docs/engines/n2system.md) |
| TYPE-MOON | Melty Blood 2002, etc | [ Guide](docs/engines/type-moon.md) |
| YOX | Musicus!, etc | [ Guide](docs/engines/yox.md) |
| YU-RIS | Maggot Baits, Erewhon, etc | [ Guide](docs/engines/yuris.md) |
---
 **[Full Documentation Index ](docs/README.md)**  —
 **[Rance String Name Guide ](https://github.com/Jannabie/Niche-Studio-Weirdo/blob/main/Rance%20Guide/Rance%20Name%20Guide.md)** — **[Rance X Scripting Guide ](https://github.com/Jannabie/Niche-Studio-Weirdo/blob/main/Rance%20Guide/Rance%20Script%20Guide.md)**

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
