Editing Minato (New) / ACV1 files
---------------------------------

Newer Minato Soft titles (such as Wagamama High Spec) utilize ACV1 `.dat` archives. These archives require explicit cryptographic keys to decrypt their contents and script structures.

Configuration
-------------

Before initiating any operations, verify the cryptographic parameters in the UI. 
- **Master Hex Key:** The primary decryption key for the ACV1 archive (Default: `0x8B6A4E5F`).
- **Script Hex Key:** The specific key required for parsing text structures (Default: `0x3793B711`).
- **Zlib Compression Level:** Determines the compression ratio when repacking (0 to 9). 

The default keys provided in the tool correspond to standard Minato New engine titles. Do not modify these unless working with a non-standard title.

Unpacking archives
------------------

Use the **Unpack .dat** tool to decrypt and extract the archive to a local folder. You can use the **Repack .dat** tool to re-encrypt a folder back into a valid ACV1 archive.

Parsing scripts
---------------

Extracted script files can be parsed into two translation formats:
1. **JSON:** Standard structured format.
2. **CSV:** Spreadsheet-compatible format, ideal for collaborative translation workflows.

Use **Parse → JSON** or **Parse → CSV** on the target script file. Once translation is complete, use **Inject → Script** to compile the modified text back into the original engine script format.
