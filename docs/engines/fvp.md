# FVP Engine

**Tab Name:** FVP Engine  
**Powered by:** [vn-tools / fvp-tools](https://github.com/vn-tools/fvp-tools)  
**File Formats:** `.bin`, `.hcb`, NVSG (`.hzc`)

---

##  1. BIN Archive Extract & Repack

The `.bin` archive contains the game's assets. 

### Extracting
1. Under **BIN ARCHIVE**, click **Browse** next to **EXTRACT**.
2. Select the `.bin` archive you want to extract.
3. Click **Extract BIN**. The files will be extracted to a new folder named `[filename]_extracted` next to the original `.bin`.

### Repacking
1. Under **BIN ARCHIVE**, click **Browse** next to **REPACK**.
2. Select the folder containing your modified files.
3. Click **Repack to .BIN**. 

---

##  2. HCB Script Decompile & Compile

The `.hcb` files contain the game's compiled scripts and dialogue.

### Decompiling (Extracting Text)
1. Under **HCB SCRIPT**, click **Browse** next to **DECOMPILE**.
2. Select the `.hcb` file.
3. Click **Decompile HCB**. 
4. The tool will generate two files in the same folder: `strings.txt` (the translatable text) and `script.dat` (the script bytecode structure).
5. Translate the text inside `strings.txt`.

### Compiling (Injecting Text)
1. Under **HCB SCRIPT**, click **Browse** next to **COMPILE**.
2. Select the **folder** that contains your translated `strings.txt` and the `script.dat`.
3. Click **Compile to .HCB**. 
4. A new file named `script_compiled.hcb` will be generated in that folder.

---

##  3. NVSG Image Decode & Encode

FVP games use the NVSG image format (often without extensions or using `.hzc`).

### Decoding (NVSG -> PNG)
1. Under **NVSG IMAGE**, click **Browse** next to **DECODE**.
2. Select the NVSG / `.hzc` image file.
3. Click **Decode to PNG**. The image will be converted so you can edit it in Photoshop or GIMP.

### Encoding (PNG -> NVSG)
1. Under **NVSG IMAGE**, click **Browse** next to **ENCODE**.
2. Select your edited `.png` file.
3. Click **Encode to NVSG**. The image will be converted back to the `.hzc` format, ready to be repacked.

---

```text
Full Workflow Summary:
1. Extract `.bin` archive to get `.hcb` and `.hzc` files.
2. Decompile `.hcb` into `strings.txt` and `script.dat`.
3. Translate `strings.txt`.
4. Compile back into a new `.hcb`.
5. Decode `.hzc` images to `.png` to edit, then encode them back.
6. Repack all the files back into a new `.bin` archive.
```
