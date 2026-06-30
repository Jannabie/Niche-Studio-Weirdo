# TYPE-MOON

Engine ini dipakai di rilis terbaru TYPE-MOON, contohnya Melty Blood: Type Lumina, di mana datanya dikompres jadi archive berformat `.p`.

Sistem unpack dan repacknya standar banget, kamu tinggal pilih archive `.p` buat dibongkar, lalu repack kalau filenya udah kamu modifikasi. Script dialog gamenya sendiri berbentuk file teks biasa (`.TXT`), yang bikin gampang buat diterjemahin.

Tapi, ada peringatan krusial banget buat game ini! Engine ini bener-bener buta sama alfabet Latin biasa (half-width ASCII). Kalau kamu nekat ngetik alfabet normal di dalam file `.TXT`-nya, teks di dalem gamenya bakal ancur lebur. Kamu **wajib** pakai teks berformat Fullwidth (Zenkaku).

Biar kamu nggak pusing, di tab ini udah disediain fitur converter. Tinggal ketik aja kalimat Inggris/Indonesiamu (misalnya "Shiki Tohno"), nanti bakal otomatis diubah ke wujud fullwidth ("Ｓｈｉｋｉ　Ｔｏｈｎｏ"). Kamu tinggal copy teks yang udah di-convert itu dan paste ke file `.TXT` scriptnya.
