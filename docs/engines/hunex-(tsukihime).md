Editing Tsukihime Remake files
------------------------------

The Tsukihime Remake (HuneX engine variant) consolidates all story and dialogue data into a single master file named `script_text.mrg`.

Extracting script data
----------------------

To access the dialogue:

1. Select the `script_text.mrg` file.
2. Click **Extract → .TXT** to generate a plaintext file containing all script strings, with one entry per line.
3. It will gave you all mapping folder for all the scene.


Open the resulting `.txt` file in a text editor (e.g., Notepad++) and translate the entries. 

**WARNING:** The total number of lines in the `.txt` file must exactly match the original. Adding or removing lines will corrupt the index and cause the repack process to fail or the game to crash.

Repacking the MRG file
----------------------

Once you have finished editing the text, use the **Repack → .MRG** function, select your modified `.txt` file, and specify the output path to generate a new `script_text.mrg` file for the game.
