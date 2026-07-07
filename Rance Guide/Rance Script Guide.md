# Rance 10 — Script & Translation Guide
> Panduan lengkap untuk menerjemahkan teks dalam game Rance 10 (Dialog, Julukan, dan Nama Quest).

---

## 1. Mengubah Julukan Karakter (Warna Kuning)

Di dalam game, biasanya ada julukan (Title/Katagaki) yang muncul di atas nama karakter dengan warna kuning saat mereka bicara (misalnya: *Demon King* Rance).

Julukan ini disimpan di dalam file **`Rance10.txt`**.

### Caranya:
Cari variabel dengan nama `; Ｔ肩書き` (T Katagaki) di dalam script. Nanti lo bakal nemu baris kode yang bentuknya seperti ini:

```
; Ｔ肩書き = "魔王"
```

Tinggal ganti teks di dalam tanda kutip dengan terjemahan yang lo mau:

```
; Ｔ肩書き = "Demon King"
```
Setelah di-build, teks kuning di atas nama karakter tersebut akan otomatis berubah.

---

## 2. Mengubah Judul Quest

Judul-judul quest yang ada di menu pilihan game tidak disimpan di `Rance10.txt`, melainkan di dalam **`Rance10EX.txt`**.

### Caranya:
1. Buka file `Rance10EX.txt` (pastikan pakai encoding **Shift-JIS**).
2. Cari tabel dengan nama `table クエスト情報` (Quest Information Table).
3. Bentuk tabelnya akan seperti ini:

```
table クエスト情報 = {
	{ indexed int Id, string 識別名, int 種別, string クエスト名, string 説明１, string 説明２, string 説明３, string 説明４, int 地域 = 0, int リザルト有無 = 0, int 有利所属 = 0, int 有利属性１ = 0, int 有利属性２ = 0, string 選択画像 = "", int クエストアウト可能 = 0 },
	{ 2, "ホルスの宇宙戦艦", 10, "Reruntuhan Kapal Perang Raksasa", "", "", "", "", 0, 0, 0, 1, 1, "", 0 },
```

4. Fokus ke elemen **keempat** dalam baris data tersebut (contoh di atas yang sudah diganti jadi `"Reruntuhan Kapal Perang Raksasa"`).
5. Ganti teks tersebut dengan judul quest yang lo inginkan. Sisanya biarkan sama persis (jangan hapus koma atau angka lainnya).

---

## 3. Format Aman Mengganti Kagikakko (「 」) dengan Kutip Ganda (" ")

Ini adalah trik yang sangat penting kalau lo mau mengganti tanda kurung Jepang `「` dan `」` dengan tanda kutip ganda ala Inggris `" "` tanpa memicu error compile (`Unterminated string literal`).

Di dalam script `Rance10.txt`, teks sering kali terpotong menjadi dua baris atau lebih.

### Format yang Benar:

Kalau lo membagi satu kalimat panjang ke dalam dua baris kode, format tanda kutipnya **wajib** seperti ini:

```
;m[128991] = "\"Apa-apaan, sih?"
;m[128992] = "Setelah kita masuk ke dalam kapal perang raksasa ini...\""
```

### Kenapa Format Ini Berhasil? (Penjelasan Logika)

- **Baris Pertama:** `";m[128991] = "\"Apa-apaan, sih?"`
  - `"` (pertama) → Pembuka string script (Kode wajib engine).
  - `\"` → Memunculkan tanda kutip literal `"` di dalam game sebagai pembuka dialog.
  - `Apa-apaan, sih?` → Teks dialog.
  - `"` (terakhir) → Penutup string script (tanpa backslash, ini menutup string untuk baris tersebut).
  - *Hasil di game: `"Apa-apaan, sih?`*

- **Baris Kedua:** `";m[128992] = "Setelah kita masuk ke dalam kapal perang raksasa ini...\""`
  - `"` (pertama) → Pembuka string script.
  - `Setelah kita masuk...` → Teks dialog lanjutan.
  - `\"` → Memunculkan tanda kutip literal `"` di dalam game sebagai penutup dialog.
  - `"` (terakhir) → Penutup string script.
  - *Hasil di game: `Setelah kita masuk ke dalam kapal perang raksasa ini..."`*

Dengan format silang seperti ini, lo sukses membuat tanda kutip pembuka di kalimat pertama dan tanda kutip penutup di kalimat kedua, **tanpa merusak struktur kode dari AliceTools**. 

---
*Guide ini ditulis berdasarkan observasi langsung dari struktur file Rance10.txt dan Rance10EX.txt yang di-dump menggunakan AliceTools.*
