Editing Fuzz Inc. files
-----------------------

The Fuzz Inc. engine, used in modern releases such as Fate/stay night Remastered, packages game data in encrypted `.epk` archives.

Setting decryption keys
-----------------------

Before modifying any archives, you must configure the engine's cryptographic keys:

1. Locate the `decryptKey.bin` file within your game directory.
2. Locate the game's executable (e.g., `main.exe`).
3. Set both paths in the tool configuration to allow decryption operations.

Decrypting and extracting EPK files
-----------------------------------

Once the keys are configured, use the **Decrypt EPK** function on your target `.epk` file. 

To translate dialogue:
1. Export the decrypted file into an editable `.json` format using **Export → JSON**.
2. Translate the values within the JSON file.
3. Inject the translated JSON back into the archive structure using **Inject → EPK**.

Building Steam patches
----------------------

For Steam releases, modifying base game files directly may trigger integrity verifications. The tool provides a **Build Patch** function to generate a Steam-compatible patch file that safely overrides the original binaries.
