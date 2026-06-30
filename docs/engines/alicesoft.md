Editing Alicesoft files
-----------------------

The Alicesoft engine (used in the Rance series and Evenicle) utilizes several distinct file formats for its scripts, databases, and assets.

Editing .ain script files
-------------------------

Story logic and text are stored in `.ain` files. To extract the text:

1. Use the AIN tool to dump the script. This will generate a text file containing all translatable strings.
2. Translate the text. The tool automatically supports an "uncomment" feature, allowing you to easily strip `;m[` or `;s[` prefixes to streamline translation.
3. Rebuild the edited text file back into a new `.ain` file.

Editing .ex database files
--------------------------

Database entries are stored in `.ex` files.

1. Dump the `.ex` file to generate a readable `.x` structure file.
2. Edit the generated file. Note that `.ex` files cannot be modified directly; they must be parsed and rebuilt.
3. Rebuild the modified `.x` file back into an `.ex` file.

Managing .afa and .ald archives
-------------------------------

Assets and media are packaged into `.afa` and `.ald` archives. You can unpack these archives to a directory, modify the contents, and repack them using the archive tool.

Editing .cg images
------------------

For images, the engine uses the `.cg` format. You can convert `.cg` files to `.png` for editing in standard image editors, and then encode them back to `.cg` before repacking the archive.
