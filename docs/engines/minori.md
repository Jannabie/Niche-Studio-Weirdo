Editing Minori files
--------------------

The Minori engine, utilized in titles like the ef series, eden*, and Supipara, packages assets in `.paz` archives.

**CRITICAL CONFIGURATION:** The file structure and encryption matrix of `.paz` archives vary significantly depending on the specific game and localization version. You must select the correct **Game Index** from the dropdown menu before performing any operations.

Using an incorrect index will result in catastrophic archive corruption during packing or unpacking.

Unpacking PAZ archives
----------------------

1. Select the appropriate **Game Index** for your target game.
2. Select the target `.paz` archive.
3. Select an output directory.
4. Click **Unpack .paz**.

Repacking PAZ archives
----------------------

1. Ensure the **Game Index** matches the one used during unpacking.
2. Select the directory containing your modified files.
3. Click **Repack Folder** to compile the directory back into a `.paz` archive.

Supported Game Indices
----------------------
* `0` - ef – the first tale (JP)
* `1` - ef – the first tale (EN)
* `7` - eden* (JP)
* `8` - eden* (EN Steam)
* `10` - Mashiro Iro Symphony (JP)
* *(Refer to the application UI for the complete list of supported indices)*
