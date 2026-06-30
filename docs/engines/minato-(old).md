Editing Minato (Old) / Majikoi files
------------------------------------

Older Minato Soft titles, including the Majikoi series, store data in `.pac` archives and manage dialogue via SEG blocks inside `.bin` scripts.

PAC Archive operations
----------------------

Use the PAC Archive Operations panel to manage asset containers:
1. Select the `.pac` file.
2. Select an output directory.
3. Click **Unpack PAC** to extract the contents.
4. When modifications are complete, select the source folder and click **Repack PAC** to compile a new archive.

BIN Script operations
---------------------

Dialogue text is stored within `.bin` files as segmented (SEG) data blocks.

1. Select your target `.bin` file.
2. Click **Extract SEG** to dump the text blocks into an editable format.
3. Translate the extracted text.

Before repacking the script, you must select the correct **Repack Mode** from the dropdown menu to match the specific version of the game you are modifying. Using an incorrect mode will result in invalid byte alignments. 

Select the original `.bin` file alongside your translated text, and click **Repack SEG** to inject the translations and build a new script file.
