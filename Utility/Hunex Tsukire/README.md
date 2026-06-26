# TsukiRe-mrg-txt

Tool untuk mengekstrak dan merepack file `script_text.mrg` dari game **Tsukihime -A piece of blue glass moon-** (Nintendo Switch).

> **Alternatif lebih mudah:** Gunakan [TsukiRe-Translator](https://github.com/Jannabie/TsukiRe-translation) jika ingin mengedit teks langsung lewat GUI tanpa perlu konversi ke file `.txt` terlebih dahulu.

---

## Apa Ini?

`script_text.mrg` adalah file arsip format MZP (`mrgd00`) yang menyimpan seluruh teks dialog game dalam encoding UTF-8. Tool ini mengekstrak isi arsip menjadi file `.txt` yang bisa diedit, lalu merepacknya kembali ke format MRG yang kompatibel dengan game.

Hasil ekstraksi diorganisir berdasarkan route sehingga lebih mudah dinavigasi: **Common Route**, **Arcueid Route**, **Ciel Route**, dan **QA**. Setiap baris teks dalam file hasil ekstrak dikaitkan dengan ID offset uniknya agar proses repack bisa menghitung ulang seluruh pointer secara presisi tanpa merusak struktur internal engine.

---

## ⚠️ Catatan Penting
* **Game Dump:** Game harus di-dump terlebih dahulu secara mandiri untuk mendapatkan file yang diperlukan.
* **Versi Game:** Tool ini hanya bisa dilakukan dan berfungsi pada **Versi Jepang**.

---
## Perbandingan Hasil Patch


<table>
  <tr>
    <th align="center">Sebelum — Teks Jepang Original</th>
    <th align="center">Sesudah — Patch Indonesia</th>
  </tr>
  <tr>
    <td><img src="https://i.imgur.com/Fl6iTqW.png" width="350"></td>
    <td><img src="https://i.imgur.com/eEtdYFB.jpeg" width="350"></td>
  </tr>
</table>

---

## Struktur File

| File | Peran |
|---|---|
| `mrg_tool.py` | Tool utama — ekstrak dan repack MRG via GUI atau CLI |
| `mrg_editor.py` | Editor teks sederhana untuk file hasil ekstrak |
| `scene_map.json` | Peta offset ke nama scene, dibutuhkan untuk organisasi per route |

---

## Cara Pakai

### Mode GUI

Jalankan langsung untuk membuka antarmuka grafis:

```bash
python mrg_tool.py
```

Buka `script_text.mrg` lewat tombol yang tersedia, pilih folder output, lalu klik Extract. Setelah file `.txt` selesai diedit, buka kembali tool dan gunakan fungsi Repack untuk menghasilkan MRG baru.

### Mode CLI

```bash
# Ekstrak MRG ke folder teks
python mrg_tool.py extract script_text.mrg output/

# Repack folder teks kembali ke MRG
python mrg_tool.py repack output/ script_text_patched.mrg
```

---

## Memasang Patch ke Game (LayeredFS)

Letakkan `script_text.mrg` hasil repack di path berikut sesuai emulatornya, tanpa mengubah file ROM asli:

**Yuzu:** `%AppData%\Roaming\yuzu\load\010064101344A000\[Nama Mod]\romfs\script\`

**Ryujinx:** `%AppData%\Roaming\Ryujinx\mods\contents\010064101344a000\[Nama Mod]\romfs\script\`

---

## Requirements

Python 3.8 atau lebih baru. `tkinter` dibutuhkan untuk mode GUI dan sudah terinstall otomatis bersama Python di Windows.

---

## Disclaimer

Tool ini dibuat untuk keperluan edukasi dan lokalisasi personal. Gunakan sesuai aturan copyright dan Terms of Service dari game original.
