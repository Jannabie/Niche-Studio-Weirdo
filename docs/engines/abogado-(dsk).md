# Abogado (DSK)

Engine ini biasanya dipakai di game kayak Shuumatsu no Sugoshikata. Intinya, game ini nyimpen datanya di dalam file `.dsk`, tapi file ini selalu butuh pasangannya yaitu file indeks `.pft` supaya bisa dibaca. 

Kalau kamu mau bongkar gamenya, kamu tinggal pilih file `.pft` dan `.dsk`-nya di menu, terus tentuin folder buat ngekstraknya. Nanti semua isinya bakal keluar di folder itu. Kalau udah selesai ngedit, tinggal lakuin hal yang sama di bagian repack buat bikin file `.dsk` dan `.pft` yang baru.

Nah, untuk bagian teksnya, gamenya pakai file `.scf`. Kamu bisa parse file `.scf` ini jadi JSON supaya gampang ditranslate. Begitu kelar translate di JSON-nya, tinggal inject balik JSON itu ke file `.scf` aslinya, terus masukin lagi ke folder yang mau di-repack ke `.dsk`. Gampang kan? Jangan lupa cek juga tool Abogado KG kalau kamu mau ngedit gambarnya.
