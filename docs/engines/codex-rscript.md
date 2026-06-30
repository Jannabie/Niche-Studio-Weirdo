Editing codeX RScript files
---------------------------

The codeX RScript engine utilizes `.gsc` bytecode files for its scenarios and scripts.

Exporting .gsc files
--------------------

First, set the target encoding (e.g., Shift-JIS for standard Japanese releases) and choose whether to process a single file or a batch directory.

1. Export the `.gsc` bytecode into an editable JSON format.
2. The output JSON will contain all translatable text strings.

Editing the JSON
----------------

Modify only the string values within the JSON file. Modifying structural keys or removing entries will result in a corrupted bytecode injection.

Before proceeding with a full translation, it is highly recommended to run the **Verify Roundtrip** check on an unmodified export. This ensures that the engine variant is fully supported and that byte-identical compilation is possible.

Importing and rebuilding
------------------------

Once you've finished editing the JSON, use the **Import JSON → GSC** function to recompile the translation back into `.gsc` bytecode.
