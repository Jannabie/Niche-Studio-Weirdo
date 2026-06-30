# YOX

Engine YOX ini, yang dipakai di game Musicus!, punya proses terjemahan 4 tahap yang cukup panjang. Sistem mereka super ketat, jadi kamu bener-bener harus ngikutin tahapnya secara berurutan dan jangan lompat-lompat!

Tahap pertama, kamu unpack file `.dat` bawaan game. Hasil dari unpack ini adalah file `.dec` (decrypted) dan yang paling penting, ada file `manifest.json`.

Lanjut tahap kedua, pakai file `.dec` tadi buat mengekstrak scriptnya jadi `.json`. Di dalam file JSON ini baru kamu bisa ngetranslate dialog-dialog gamenya.

Nah, ini bagian bahayanya: selama kamu ngetranslate, jangan pernah pindahin atau nge-rename file `manifest.json` dari foldernya. File ini itu nyimpen konteks sesi kerjaan kamu. Di tahap ketiga, begitu terjemahannya beres, tinggal import JSON terjemahan tadi biar dijadiin file `.dec` baru.

Tahap terakhir, kamu repack file `.dec` yang udah ditranslate tadi jadi file `.dat` lagi. Kalau kamu ngelanggar urutannya atau nyampur-nyampur file dari sesi lain, file `manifest.json` bakal ngambek dan proses repack bakal gagal total.
