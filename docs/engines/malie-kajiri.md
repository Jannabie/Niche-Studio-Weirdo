Editing Kajiri Kamui Kagura files
---------------------------------

This toolchain is exclusively designed for Kajiri Kamui Kagura (KKK) and the Malie engine variant used in Dies irae.

Installing the Translation Layout Patch
---------------------------------------

Japanese versions of the game utilize vertical text boxes. To properly render translated alphanumeric text from left to right, you must install a layout patch.

1. Select the root directory of the game.
2. Select **Horizontal ADV** (standard) or **Vertical ADV** (original).
3. Click **Install Base Patch**. This only needs to be executed once per installation.

Building the Translation
------------------------

Once you have prepared your translated scripts, follow the strict 3-step compilation pipeline:

1. **Wordwrap (Optional):** Execute this step to automatically wrap long dialogue lines within your `dependencies/` folder to prevent text from rendering outside the message window.
2. **Compile Script:** Execute this step to invoke `Malie_Script_Tool.exe`. This will compile your raw text into the engine's required `exec.dat` binary format.
3. **Pack data6.dat:** Execute this step to pack the compiled data into the final `data6.dat` archive. 

Once `data6.dat` is generated, place it in your game's data directory.
