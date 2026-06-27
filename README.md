# Niche Studio Weirdo

A centralized WPF toolset for reverse-engineering and translating Japanese visual novels, wrapped in a macOS-inspired dark UI. Supports a wide range of VN engines.

## Supported Engines

Abogado, Buriko (BGI), CodeXR, Fuzz Inc., HuneX, Leaf (WA2), Malie, Minato (Majikoi/WagaHime), Minori, TYPE-MOON (Melty Blood), YOX (Musicus).

Each engine tab covers the full pipeline — script extraction, JSON translation, and repacking — plus image and archive tools where applicable.

## Buriko (BGI) — サクラノ詩 ～春ノ雪～ Support

Supports decoding compressed `.SKP` scripts (DSC FORMAT 1.00), extracting Shift-JIS dialogue and character names to JSON, editing, and repacking back into the game.

## UI

Emulates a macOS terminal with dark mode, tab navigation, and `SF Mono Medium` font. Install the font from the `Font/` directory for the best experience.

![Tools Interface](Tools%20Interface/image.png)

## Build

Requires .NET 8.0 SDK and Windows (WPF). Run `dotnet publish` or open in Visual Studio. Pre-compiled executables are distributed separately via Releases.
