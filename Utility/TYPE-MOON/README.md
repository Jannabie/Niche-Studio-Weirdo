# Melty Blood 2002 — Archive Tools & Editor

Tool buat modding dan lokalisasi **Melty Blood (2002)** — dari ekstrak arsip, edit teks lewat GUI, sampai repack balik ke format aslinya.

**Requirements:** Python 3.10+, no external deps (kecuali `tkinter` buat GUI, biasanya udah ada di Python Windows).

---

## Isi Repo

| File | Kegunaan |
|---|---|
| `mb_core.py` | Library inti + CLI buat unpack/repack arsip `.p` |
| `mb_editor.py` | GUI editor buat translator — tanpa perlu buka terminal |

Keduanya kompatibel dengan format arsip *Mirror Moon English Patch* (Shift-JIS).

---

## Cara Pakai

### GUI — Disarankan untuk Translator

```bash
python mb_editor.py
```

Klik **Open Archive (.p)** → pilih `data04.p`. Panel kiri bakal nampilin semua file script di dalam arsip. Pilih salah satu, isi terjemahan di kotak yang muncul tiap baris dialog, selesai klik **Repack Archive**.

Fitur yang ada: syntax highlighting (biar gampang bedain command vs teks), find & replace global, pelacak progres, dan export/import terjemahan buat kolaborasi tim.

### CLI — Untuk Otomasi

```bash
# Ekstrak arsip ke folder
python mb_core.py unpack data04.p extracted/

# Repack folder hasil ekstrak
python mb_core.py repack extracted/ data04_new.p

# Lihat isi arsip (offset, ukuran, dsb) tanpa ekstrak
python mb_core.py info data04.p
```

---

## ⚠️ Wajib Baca: Huruf Fullwidth

Game ini pakai font renderer Shift-JIS yang **nggak ngerti huruf Latin biasa (half-width)**. Kalau nulis terjemahan pakai `A`, `B`, `C` biasa, teksnya nggak bakal muncul di game — atau muncul tapi jadi karakter rusak.

Semua terjemahan harus pakai **karakter Fullwidth (Zenkaku)**:

```
❌ Half-width : "Di awal bulan Agustus."
✅ Fullwidth  : "Ｄｉ　ａｗａｌ　ｂｕｌａｎ　Ａｇｕｓｔｕｓ．"
```

Cara paling gampang: ubah mode input IME ke Zenkaku, atau pakai tabel konversi. Editor GUI sudah nampilin teks apa adanya jadi kamu bisa langsung kontrol hasilnya.

---

## Tentang `_manifest.json`

Setiap kali arsip diekstrak, file `_manifest.json` otomatis dibuat di folder hasil ekstrak. File ini nyimpen urutan file asli dan flag header — dua hal yang dibutuhkan supaya repack bisa menghasilkan arsip yang identik byte-per-byte dengan aslinya.

**Jangan hapus atau ubah file ini.** Tanpa `_manifest.json`, repack nggak bisa jalan.

---

## Struktur `data04.p`

File arsip utama yang perlu diedit untuk lokalisasi. Total 189 file (~40 MB):

| Tipe | Jumlah | Isi |
|---|---|---|
| `.TXT` | 62 | Script dialog (±10.676 baris) — **yang perlu diedit** |
| `.EX3` | 107 | Data gambar/sprite |
| `.WAV` | 9 | Audio |
| `.FNT` | 1 | Data font game |

File selain `.TXT` nggak perlu disentuh kecuali kamu mau modding grafis atau audio.

---

## Format Script

File `.TXT` pakai format script internal. Yang boleh diterjemahkan cuma baris dialog/narasinya:

- `// ...` — komentar, **skip**
- `EF`, `GW`, `WI`, `MD`, `BP`, dsb. — perintah game, **jangan diubah**
- Baris yang diawali spasi atau karakter Jepang — **ini yang diterjemahkan**

---

## Proof of Concept

| Screenshot |
|:---:|
| ![Editor in action](https://i.imgur.com/UEhFLTl.png) |
| *GUI editor berjalan dengan terjemahan teraplikasi* |

---

## Disclaimer

Tool ini dibuat untuk keperluan edukasi, penelitian, dan lokalisasi personal. Pastikan penggunaannya sesuai dengan aturan copyright dan ToS game original.
