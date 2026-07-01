# Diesel Engine (Nitroplus NPK)

Tool ini ditujukan buat bongkar dan pasang file `.npk` yang sering dipakai di game-game bikinan Nitroplus (biasa dikenal dengan Diesel Engine), contohnya kayak **The Song of Saya (Saya no Uta)**, **Tokyo Necro**, atau **DRAMAtical Murder**.

Sebenernya gampang banget makenya, karena di belakang layar tool ini ngandelin **NPK3Tool** yang udah canggih buat nanganin kompresi dan enkripsi game-game Nitroplus.

## Cara Penggunaan

Langkah-langkahnya simpel:

1. **Pilih Target Game / Profile**
   Di bagian atas, kamu *wajib* banget milih game apa yang lagi kamu kerjain. Kenapa? Soalnya setiap game Nitroplus itu punya kunci enkripsi yang beda-beda. Kalau kamu salah milih profil, filenya bakalan rusak pas diekstrak atau gagal di-repack. Kalau kamu ngerjain *Saya no Uta*, pilih aja opsi **5: The Song of Saya (Steam)**.

2. **Ekstrak File NPK**
   - Di bagian "Extract", masukin file `.npk` ori dari gamenya (misalnya `script.npk` atau `cg.npk`).
   - Pilih folder tempat kamu mau naruh hasil ekstraksinya.
   - Klik **Extract → Folder**, nanti isinya bakal kebongkar semua ke sana.

3. **Repack Jadi NPK Baru**
   - Setelah kamu ngedit isi folder (misal nerjemahin teks atau ngedit gambar), masuk ke bagian "Repack".
   - Pilih folder hasil ekstraksi yang udah kamu edit.
   - Pilih nama dan lokasi buat file `.npk` barunya (otomatis dibikin dengan akhiran `_new.npk` kalau dikosongin).
   - Klik **Repack → .NPK**, dan tunggu prosesnya sampai selesai. File barunya ini tinggal kamu timpa ke folder gamenya.

> **Catatan Tambahan**: Pastikan kamu *backup* file `.npk` yang asli sebelum nimpa filenya di folder instalasi game!
