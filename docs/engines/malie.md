Editing Malie Engine files
--------------------------

The Malie Engine is commonly used in visual novels such as Sharin no Kuni, G-Senjou no Maou, and Kami no Rhapsody. Game data is packaged within encrypted `.dat` or `.lib` archives.

Decrypting archives
-------------------

Before you can edit the game data, you must decrypt the archive. Use the **Decrypt Archive** tool to process the `.dat` or `.lib` file into a standard directory format. Once your modifications are complete, use the **Re-encrypt Archive** tool to secure the directory back into its original format.

Script Translation
------------------

The Malie engine isolates character names and dialogue into separate data structures. 

1. Select the directory containing the decrypted script files.
2. Use **Export Names** to generate a translation file for character names.
3. Use **Export Dialog** to generate a translation file for story dialogue.
4. Translate the strings within the exported files.

**CRITICAL PATCHING ORDER:** When injecting your translated files back into the engine scripts, you must patch the files in the correct sequence to avoid structural corruption.
1. First, click **Patch Names** and select your translated names file.
2. Second, click **Patch Dialog** and select your translated dialogue file.

Editing MGF graphics
--------------------

Image assets utilize the proprietary `.mgf` (Malie Graphics Format). 

Use the **MGF → PNG** tool to decode the image for editing in a standard image editor. Once modified, use **PNG → MGF** to encode the image back to its original format. Ensure you place the new `.mgf` file back into the decrypted archive folder before re-encrypting.
