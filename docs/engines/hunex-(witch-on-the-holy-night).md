Editing Mahoyo Remastered files
-------------------------------

Witch on the Holy Night (Mahoyo) Remastered utilizes `.hfa` archives as its primary data container. 

Unpacking HFA archives
----------------------

1. Select your `.hfa` archive file.
2. Select an output directory.
3. Click **Unpack HFA** to extract the archive contents to your specified folder.

Editing CTD, CBG, and MZP files
-------------------------------

Inside the archive, you will find several proprietary file types:
* `.ctd` files contain text and script data.
* `.cbg` files are compressed background images.
* `.mzp` files contain sprite and UI images.

To edit these files:
1. Select the specific `.ctd`, `.cbg`, or `.mzp` file using the single-file tool.
2. Click **Extract** to convert the file into an editable format (such as `.txt` for scripts or `.png` for images).
3. Once you've finished editing the output file, use the **Repack** function to re-encode it back into its original format.

Rebuilding HFA archives
-----------------------

After replacing the files within your unpacked folder with their modified counterparts, select the directory and click **Repack HFA** to compile a new archive for the game.
