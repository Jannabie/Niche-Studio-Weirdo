Editing Buriko (BGI) files
--------------------------

The Buriko engine (BGI), used in games like Higurashi, Umineko, and Sakura no Uta, relies on `.arc` archives and `.sc` script files.

Unpacking .arc archives
-----------------------

Game assets are bundled in `.arc` files. Use the ARC tool to extract the archive into a directory. Once you have modified the files, you can pack the directory back into a new `.arc` file.

Editing .sc script files
------------------------

Dialogue and scenarios are compiled into `.sc` files. To translate these scripts:

1. Use the Script tool to parse the `.sc` file into a standard `.json` file.
2. Open the `.json` file and translate the dialogue strings.
3. Once you've finished editing, use the tool to inject the `.json` back into the `.sc` file.

The tool automatically handles byte offsets and pointer recalculations during the injection phase, ensuring the rebuilt script is structurally valid.
