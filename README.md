# Niche Studio Weirdo

Niche Studio Weirdo is a comprehensive, centralized toolset for reverse-engineering and modding Japanese visual novels from various engines, wrapped in a sleek, modern, macOS-CLI inspired aesthetic.

## Features
- **Abogado**: Parse `.SCF` scripts, inject JSON translations, convert `PNG ↔ KG`, and patch/rebuild `.DSK` archives.
- **Buriko (BGI)**: Extract, decompile, translate, and repack BGI scripts and archives.
- **CodeXR**: Extract `.bin`, export `.gsc` to JSON, and repack.
- **Fuzz Inc. (FSN)**: File extraction and `.pck` packaging.
- **HuneX & HuneX (Mahoyo)**: Parse `script.bin`, insert translations, extract/repack `.hfa` archives. Includes specialized tools for Mahoyo `.mzp` (images), `.cbg`, and `.ctd` scripts with heavily optimized KDTree palette matching and custom MZX RLE compression.
- **Leaf (WA2)**: File extraction, text translation, and script compiling.
- **Malie & Malie Kajiri**: Decrypt `.dat`, convert graphics (`PNG ↔ MGF`), and parse script files.
- **Minato (New & Old)**: Manage `Majikoi` and `WagaHime` formats, extract bins, and reconstruct archives.
- **Minori**: Decrypt `.pck`, handle scripts, and repack.
- **TYPE-MOON (Melty Blood)**: Decrypt `.dat`, extract scripts, rebuild `.dat`.
- **YOX (Musicus)**: Export/import `.dat`, translation tools.

## UI & Design
The user interface is designed to emulate a clean macOS terminal environment:
- Solid dark mode colors with a minimalist layout
- Tab-based navigation imitating standard top-bar menus
- Perfectly rounded corners matching the macOS window frame (`CornerRadius=12`)

![Tools Interface](Tools%20Interface/image.png)

- **Important**: To fully experience the UI, ensure you have the `SF Mono Medium` font installed (located in the `Font/` directory).
- Interface layout references are included in the `Tools Interface/` directory.

## Source Code & Compilation
This repository contains the source code for the tool interface and UI. 
- You can compile it using Visual Studio or the .NET SDK (`dotnet publish`).
- **Executables**: Pre-compiled binaries (`.exe`) are handled separately by the repository owner via Releases.

## Requirements
- .NET 8.0 SDK (or higher) to build.
- WPF (Windows Presentation Foundation) environment (Windows only).
- Ensure `SF Mono Medium` is installed on the host system to prevent UI rendering fallbacks.
