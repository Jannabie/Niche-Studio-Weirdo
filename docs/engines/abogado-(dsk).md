Editing Abogado DSK and SCF files
---------------------------------

Abogado engine games (such as Shuumatsu no Sugoshikata) store assets in `.dsk` archive files. Each `.dsk` file is always accompanied by a `.pft` index file.

First, unpack the archive using the DSK Unpacker:
1. Select the `.pft` index file.
2. Select the `.dsk` data file.
3. Select an output directory.
4. Click **Unpack Archive**.

This will extract all data structures and files from the archive into your selected folder.

Editing script files (.scf)
---------------------------

Story scripts are stored in `.scf` files. To translate them:

1. Parse the `.scf` file into a JSON format using the **Parse → JSON** feature.
2. Open the resulting `.json` file and translate the text strings.
3. Once you've finished editing the JSON file, inject it back using the **Inject → SCF** feature to generate a new `.scf` file.

Rebuilding the archive
----------------------

Once you have finished editing your extracted files, you can rebuild the archive. Use the Repack Archive tool, select the folder containing your modified files, and specify the output path. The tool will generate a new pair of `.dsk` and `.pft` files for the game.
