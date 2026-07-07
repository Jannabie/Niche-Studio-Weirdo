# Contributing & Acknowledgements

**Niche Studio Weirdo** is a graphical frontend / workflow shell — it does not re-invent the wheel.
Almost every heavy-lifting operation under the hood is powered by open-source tools built by the
VN modding & translation community. This project wouldn't exist without them.

---

##  Bundled Third-Party Tools

The table below lists every external tool that ships inside `Utility/` along with its original
author(s) and license. Please respect each project's own license before redistributing.

| Utility Folder | Tool Name | Used For | Author / Source |
|---|---|---|---|
| `(Built-in)` | **Abogado Tools** | Extracting & repacking Abogado `.dsk`, `.scf`, `.kg` | Custom C# implementation |
| `Utility/Alicesoft/` | **alice-tools** | Extracting & editing AliceSoft engine archives (ALD, AFA, ALD files) | [nunuhara](https://github.com/nunuhara/alice-tools) |
| `Utility/Buriko/` | **BGI Translator** | Unpacking & repacking Ethornell/BGI `.arc` script archives | Custom Python tool (bundled) |
| `Utility/CodeXR/` | **codeX RScript Extractor** | Reading & repacking `.gsc` script files (Liar-Soft's codeX engine) | Custom Python tool (bundled) |
| `Utility/Diesel Engine/` | **MwareStuff** | Unpacking & repacking Mware/Diesel Engine `.npk` archives | [marcussacana](https://github.com/marcussacana/MwareStuff) |
| `Utility/DieselEngineBin/` | **NPK3Tool** | Binary helper for Diesel Engine NPK3 archive format | Part of MwareStuff |
| `Utility/FVP/` | **fvp-tools** | Extracting/Repacking `.bin`, decompiling `.hcb`, converting `.hzc` | [vn-tools](https://github.com/vn-tools/fvp-tools) |
| `Utility/Fuzz Inc/` | **Fuzz Inc. Toolkit** | Full translation pipeline for Fate/Stay Night Remastered (EPK archives) | Custom Python tool (bundled) |
| `Utility/Hunex Mahoyo/` | **WoTH Tools** | Unpacking & rebuilding Witch on the Holy Night (Mahoyo Remastered) files | Custom tool (bundled) |
| `Utility/Hunex Tsukire/` | **Tsukihime Remake Parser** | Parsing `.mrg` script archives from Tsukihime Remake | Custom tool (bundled) |
| `Utility/Hunex Tsukire Translation/` | **deepLuna** | GUI translation editor for Tsukihime Remake scripts | Custom tool (bundled) |
| `Utility/Leaf/` | **Leaf Engine Arch** | Extracting & repacking `.pak` (KCAP) archives from White Album 2 | Custom Python tool (bundled) |
| `Utility/LuckSystem/` | **LuckSystem (Yoremi Fork)** | Decompiling & compiling Key/VisualArts Luca System scripts | Fork of [wetor/LuckSystem](https://github.com/wetor/LuckSystem) |
| `Utility/LuckSystemBin/` | **LuckSystem Binaries** | Pre-built binaries for Luca System script processing | Part of LuckSystem |
| `Utility/Malie/` | **MalieToolKit** | Extracting, editing & repacking FreeMalie engine archives | Custom tool (bundled) |
| `Utility/Malie Kajiri/` | **Kajiri Kamui Kagura Toolkit** | Translation tools specific to Kajiri Kamui Kagura (Malie engine) | Custom tool (bundled) |
| `Utility/Minato New/` | **Minato Engine New Tools** | Extracting & repacking archives from newer Minato engine titles | Custom Python tool (bundled) |
| `Utility/Minato Old/` | **Minato Soft Engine Tools** | Unpacking `.pac` archives & `.bin` scripts (Majikoi series) | Custom Python tool (bundled) |
| `Utility/Minori/` | **Minori Engine Modding Archive** | Decryption keys & tools for Minori engine games (ef, eden*, etc.) | Custom tool (bundled) |
| `Utility/N2SystemBin/` | **N2System Binaries** | Archive unpacking for Key's N2System engine (Rewrite, Angel Beats, etc.) | Custom tool (bundled) |
| `Utility/RxYuris/` | **YurisTools (Source)** | YU-RIS engine file processing (XOR, YSTB, YPF) — C++ source | Custom C++ tool (bundled) |
| `Utility/RxYurisBin/` | **YurisTools (Binaries)** | Pre-built YU-RIS processing binaries | Part of YurisTools |
| `Utility/TYPE-MOON/` | **Melty Blood Archive Tools** | Unpacking `.p` archives & editing TXT scripts for Melty Blood (2002) | Custom Python tool (bundled) |
| `Utility/VNTextPatch/` | **VNTextPatch** | Extracting & injecting text in YU-RIS `.ybn` script files | [arcusmaximus](https://github.com/arcusmaximus/VNTranslationTools) · [rafael-vasconcellos (net8 fork)](https://github.com/rafael-vasconcellos/VNTextPatch-net8) |
| `Utility/YOX/` | **YOX Engine Arch** | Extracting & reinserting dialogue from MUSICUS (YOX engine) | Custom Python tool (bundled) |
| `Utility/YuRISTools/` | **YuRIS Tools** | All-in-one YPF packing/unpacking for YU-RIS engine | Custom tool (bundled) |
| `Utility/ypf-repacker/` | **ypf-repacker** | Repacking folders into YPF archives (fork of YPF Manager Tool) | Custom tool (bundled) |

---

## Special Thanks

These projects and their contributors made this toolkit possible:

- **[nunuhara](https://github.com/nunuhara)** — for `alice-tools`, a comprehensive suite for AliceSoft game modding.
- **[marcussacana](https://github.com/marcussacana)** — for `MwareStuff`, the only tool that properly handles Diesel/Mware Engine NPK archives.
- **[arcusmaximus](https://github.com/arcusmaximus)** — for `VNTranslationTools` / `VNTextPatch`, the backbone of YU-RIS script extraction and injection.
- **[rafael-vasconcellos](https://github.com/rafael-vasconcellos)** — for the `.NET 8` port of `VNTextPatch`, keeping the tool modern and dependency-light.
- **[wetor](https://github.com/wetor)** — for `LuckSystem`, the foundation for Key/VisualArts Luca System translation.
- **[vn-tools](https://github.com/vn-tools)** — for `fvp-tools`, the only open-source toolkit for FVP Engine archives and scripts.
- The **Tsukihimates** team — for foundational research on Tsukihime Remake's archive format.

---

##  Contributing to This Project

Want to add support for a new engine or fix a bug? Here's how:

1. **Fork** this repository.
2. Create a branch: `git checkout -b feature/new-engine-name`
3. Add your engine view in `Views/`, your backend logic in `Utils/`, and any tools in `Utility/`.
4. Register your new tab in `MainWindow.xaml` and `MainWindow.xaml.cs`.
5. Open a **Pull Request** describing what engine you added and which games it supports.
