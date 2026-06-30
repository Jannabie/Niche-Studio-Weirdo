# Fuzz Inc.

Engine Fuzz Inc ini lumayan unik, dipakai di rilisan baru kayak Fate/stay night Remastered. File-file mereka biasanya dibungkus di dalam archive `.epk` dan dienkripsi. 

Makanya, sebelum kamu bisa ngapa-ngapain, kamu wajib masukin kunci dekripsinya dulu. Kamu butuh file `decryptKey.bin` sama file `main.exe` dari folder gamenya biar tool ini bisa ngebaca cara mereka ngenkripsi datanya.

Kalau kunci itu udah diset, kamu tinggal pilih file `.epk` yang mau dibongkar dan klik decrypt. Dari situ, kamu bisa ekstrak teks ceritanya jadi file JSON buat ditranslate santai-santai.

Selesai ngetranslate? Inject balik JSON-nya ke EPK. Nah, khusus buat game yang rilis di Steam, tool ini juga nyediain fitur Build Patch supaya kamu bisa bikin patch yang gak bakal bermasalah sama sistem verifikasi kontennya Steam.
