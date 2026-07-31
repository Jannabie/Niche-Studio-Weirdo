# Lithium (3DS ATLUS Hacking Tools)

> A unified toolset for modifying Nintendo 3DS ROMs (`.cia`), specifically tailored for ATLUS games (like Shin Megami Tensei IV) and their proprietary formats.

##  Requirements
- 3DS Decrypted or Encrypted `.cia` files
- If using encrypted `.cia`, `seeddb.bin` must be present in the tool's directory (bundled automatically).

##  1. Archive Extraction & Building
This tool handles the standard 3DS ROM workflow.

### EXTRACT (.CIA -> ROMFS)
1. In the **Archive / ROM Builder** tab, select your `.cia` file in the **DECRYPT / UNPACK CIA** section.
2. Click **Unpack CIA**.
3. The tool will automatically decrypt the `.cia` (if encrypted), extract the `.cxi` (NCCH), and dump the `RomFS` and `ExeFS`.
4. Wait for the `DecryptedRomFS` folder to be generated.

### REPACK (ROMFS -> .CIA)
1. In the **BUILD CIA** section, provide the path to your modified `DecryptedRomFS` folder.
2. Provide the path to your extracted `ExeFS` folder.
3. Click **Build CIA** to compile the new `.cia` file ready for Citra or real hardware.

---

##  2. Script Translation (Moonbeam)
For ATLUS games like SMT IV, text is handled via proprietary formats which are parsed by the integrated `Moonbeam` script translator.

### MASS FOLDER MODE
1. In the **Script Translation** tab, check the **Mass Folder Mode** box if you are translating an entire folder of script files at once.
2. Provide the folder path.
3. Use **Export Script** to dump all strings.
4. After translating, use **Inject Script** to patch them back.

*Note: For single files, uncheck Mass Folder Mode and select the specific script file.*

---

## 3. Graphics (.BFLIM)
*(Image conversion tools for BFLIM, BIMG, etc., are currently a work in progress and not yet fully implemented.)*
