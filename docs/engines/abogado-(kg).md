Editing Abogado KG image files
------------------------------

The Abogado engine uses a proprietary `.kg` image format. To edit these images, you must first convert them to a standard format.

Converting to PNG
-----------------

Use the **KG → PNG** tool to decode the image:
1. Select the `.kg` file you wish to edit.
2. Click **KG → PNG** to generate a standard PNG file.
3. Edit the resulting `.png` file using your preferred image editor.

Rebuilding KG files
-------------------

Once you've finished editing the PNG, you must convert it back to the `.kg` format for the game engine to read it:

1. Select your modified `.png` file.
2. Provide the original `.kg` file in the tool. This step is critical, as the tool relies on the original file to accurately preserve the encoding metadata and color depth.
3. Click **PNG → KG** to generate the final image file.
