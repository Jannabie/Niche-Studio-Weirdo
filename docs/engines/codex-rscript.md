# codeX RScript

Engine codeX RScript biasanya pakai bytecode `.gsc` buat script gamenya. 

Proses translate di sini lumayan gampang tapi kamu harus hati-hati. Pertama, kamu bisa pilih mau ngerjain satu file `.gsc` doang atau langsung banyakan sefolder. Terus jangan lupa set encodingnya, biasanya sih Shift-JIS kalau gamenya masih bahasa Jepang asli.

Setelah kamu ekstrak `.gsc`-nya, kamu bakal dapet file JSON. Di file JSON ini, kamu cukup ubah teks-teks ceritanya aja. Jangan iseng ngubah struktur atau nama key-nya, nanti gamenya bisa error.

Tips penting nih: Sebelum kamu mulai translate panjang lebar, biasain buat tes verify roundtrip dulu. Caranya, coba masukin file `.gsc` asli sama file JSON hasil ekstrak (yang belum kamu apa-apain) ke fitur verify. Kalau pass, berarti filenya aman buat ditranslate. Kalau udah yakin, tinggal import lagi JSON terjemahanmu buat dijadiin file `.gsc` yang baru.
