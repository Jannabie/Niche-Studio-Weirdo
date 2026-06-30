Editing Leaf KCAP archives
--------------------------

The Leaf engine, notably used in White Album 2, packages assets in `.pak` archives built on the KCAP specification.

Workspace preparation
---------------------

**CRITICAL:** You must create an entirely isolated and empty directory to serve as your workspace. 

When rebuilding a `.pak` file, the tool recursively packages all files present in the target directory. If foreign binaries, executables, or tool artifacts are present in the workspace, they will be injected into the archive, leading to inevitable runtime crashes.

Unpacking PAK archives
----------------------

1. Select your isolated workspace directory.
2. Select the target `.pak` file.
3. Click **Unpack .pak**. 

The tool will extract all contents to the workspace while automatically preserving the original Shift-JIS filename encoding.

Repacking PAK archives
----------------------

Once you have finished modifying the extracted scripts or assets within the workspace, ensure no extraneous files have been created. Click **Repack to .pak** to compile the directory back into a valid archive format.
