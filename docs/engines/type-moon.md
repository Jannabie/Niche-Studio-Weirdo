Editing TYPE-MOON files
-----------------------

Modern TYPE-MOON releases, such as Melty Blood: Type Lumina, store their data within `.p` archives. The scripts are stored in standard plaintext `.TXT` files.

Archive operations
------------------

To extract data, select the `.p` archive and click **Unpack .p**. 
To compile your modifications, select the target folder and click **Repack .p**.

Fullwidth text constraint
-------------------------

**CRITICAL WARNING:** The game engine does not support standard half-width ASCII characters (standard Latin alphabets). Inputting standard ASCII characters directly into the `.TXT` script files will result in severe text corruption in-game. All Latin characters must be encoded in Fullwidth (Zenkaku) format.

To ensure your text renders correctly:
1. Type your standard (half-width) English or localized text into the **Input** field of the Fullwidth Text Converter.
2. The **Output** field will automatically generate the Fullwidth equivalent.
3. Click **Copy Fullwidth Text**.
4. Paste the converted text into the `.TXT` script file.

*Example:* `Shiki Tohno` -> `Ｓｈｉｋｉ　Ｔｏｈｎｏ`
