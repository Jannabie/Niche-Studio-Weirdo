Editing YOX Engine files
------------------------

The YOX engine (used in Musicus!) employs a strict four-step translation pipeline. The engine relies heavily on session context stored in a `manifest.json` file. 

**WARNING:** You must execute these steps sequentially. Altering the sequence or removing the `manifest.json` file mid-process will destroy the context required for repacking.

Step 1: Unpack DAT
------------------

Select the game's `.dat` archive and click **Unpack DAT**. 
This operation decrypts the archive, generating a `.dec` (decrypted) file and a critical `manifest.json` file in the output directory.

Step 2: Export JSON
-------------------

The tool will automatically target the `.dec` file generated in Step 1. Click **Export JSON** to extract the translatable text strings into a `.json` file.

Step 3: Translate and Import
----------------------------

Translate the strings within the generated `.json` file. Do not alter structural keys.

Ensure `manifest.json` remains in its original location relative to the `.json` and `.dec` files. Click **Import JSON** to inject your translations, generating a modified `.dec` file.

Step 4: Repack DAT
------------------

Click **Repack DAT** to re-encrypt the modified `.dec` file back into the final `.dat` archive required by the game engine.
