# MalieToolKit

A collection of tools for extracting, editing, and repacking resources from games running on the **FreeMalie / Malie Engine** (by light).

> Tested on: **Soushuu Senshinkan Gakuen: Hachimyoujin** (相州戦神館學園 八命陣)

> **NOTE: Untuk Kajiri Kamui Kagura (神咒神威神楽 / KKK):** Game ini memiliki toolkit **eksklusif tersendiri** karena perbedaan struktur arsip dan format script-nya yang unik dibanding game Malie lainnya.
> Gunakan repo khusus berikut: **[MalieKit — KKK Exclusive Toolkit](https://github.com/Jannabie/KKK)**

---

## Isi Toolkit

| Folder | Isi | Keterangan |
|---|---|---|
| `LauncherDatSource/` | Source Python CLI | UnPacker / RePacker `.dat` / `.lib` |
| `MalieExScSource/` | Source C# Script Tool | Extract & repack script dialog |
| `MalieScriptExtractor/` | `Malie_Script_Tool.exe` | Versi prebuilt script tool |

---

## Bagian 1 — Data Archive Tool (`.dat` / `.lib`)

Tool ini untuk membuka dan merepak arsip utama game, seperti `data.dat`, `sound.dat`, dll.

### Requirements

- Python 3.10+
- Dependensi: `tqdm`

```bash
pip install tqdm
```

### Menjalankan CLI

```bash
cd LauncherDatSource
python cli_launcher.py
```

Atau gunakan versi prebuilt yang bisa didownload di:

**[Releases v1.0 → Malie_UnRePacker_Tool_CLI.exe](https://github.com/Jannabie/MalieToolKit/releases/tag/v1.0)**

### Menu CLI

```
[1] Dekripsi tahap 1 (.dat saja)
[2] Ekstrak penuh (.lib/.dat)
[3] Repack plain (.dat saja)
[4] Konversi MGF ↔ PNG
[Q] Keluar
```

---

### Penjelasan Menu

#### [1] Dekripsi Tahap 1
Mendekripsi file `.dat` terenkripsi menjadi file `_plain.dat` (belum diekstrak isinya).
Berguna untuk mengintip isi raw sebelum ekstrak penuh.

**Input:** path ke file `.dat`  
**Output:** `namafile_plain.dat` di lokasi yang sama

---

#### [2] Ekstrak Penuh
Membuka arsip `.dat` atau `.lib` dan mengekstrak seluruh isinya.
Otomatis membuat file metadata `namafile_entries.json` yang **wajib disimpan** untuk keperluan repack.

**Input:** path ke file `.dat` / `.lib`, path folder output  
**Output:** semua file terekstrak + `namafile_entries.json`

Format yang didukung:

| Format | Keterangan |
|---|---|
| `.ogg` | Audio, didekripsi langsung |
| `.png` / `.pn` | Gambar PNG |
| `.mgf` | Format gambar khusus Malie, disimpan as-is |
| `.dzi` | Metadata gambar tiled |
| `.svg` | Grafis vektor |
| `.csv` / `.txt` | Teks / data |
| `.mpg` | Video |
| `.swf` | Flash |
| Lainnya | Disimpan as-is (termasuk `exec.dat`, dll.) |

---

#### [3] Repack Plain
Merakit ulang file-file yang sudah diedit kembali menjadi `.dat`.

> CATATAN: Hanya mendukung `.dat` plain (tidak terenkripsi). Repack `.dat` terenkripsi belum didukung.

**Input:**
- Folder berisi file-file sumber
- Nama file `.dat` output
- Path ke file metadata `.json` (dari hasil ekstrak)

---

#### [4] Konversi MGF ↔ PNG
Mengonversi gambar antara format `.mgf` (format gambar Malie) dan `.png` standar.

**MGF → PNG:**

```bash
# Lewat CLI interaktif pilih [4], atau langsung:
python execution/mgfpng_change.py namafile.mgf --to-png
```

**PNG → MGF:**

```bash
python execution/mgfpng_change.py namafile.png --to-mgf
```

---

## Bagian 2 — Script Tool (Dialog & Strings)

Tool ini untuk mengekstrak dan mengimpor teks dialog serta nama karakter dari file script game (biasanya `exec.dat` di dalam folder `system/`).

### Requirements

- .NET 8 Runtime (untuk menjalankan `.exe`)
- Atau .NET 8 SDK (untuk build dari source di `MalieExScSource/`)

### Build dari Source

```bash
cd MalieExScSource
dotnet build
```

Atau jika path dotnet tidak terdeteksi otomatis:

```bash
"C:\Program Files\dotnet\dotnet.exe" build
```

---

### Perintah CLI

```
Malie_Script_Tool.exe -d  -in [input.dat] -out [output.txt]        → Disassemble script
Malie_Script_Tool.exe -a  -in [input.dat] -out [output.txt]        → Export semua strings (termasuk nama karakter)
Malie_Script_Tool.exe -s  -in [input.dat] -out [output.dat] -txt [input.txt]  → Import strings
Malie_Script_Tool.exe -e  -in [input.dat] -out [output.txt]        → Export dialog
Malie_Script_Tool.exe -i  -in [input.dat] -out [output.dat] -txt [input.txt]  → Import dialog
```

---

### Format File Teks

Setiap entri memiliki dua baris:

```
◇00000000◇Teks asli (jangan diubah)
◆00000000◆Teks terjemahan (edit di sini)
```

---

### PENTING: Cara Mengganti Nama Karakter di Malie Engine

Di Malie Engine, **nama karakter TIDAK disimpan di segment dialog biasa**, melainkan di string segment. Jika langsung export dialog (`-e`) lalu edit nama di sana dan import (`-i`), nama karakter di game **tidak akan berubah**.

**Urutan yang benar:**

```
1. Export strings dulu
   Malie_Script_Tool.exe -a -in exec.dat -out exec_strings.txt

2. Edit nama karakter di exec_strings.txt
   Cari baris ◆ yang berisi nama asli, ganti dengan nama terjemahan

3. Import strings (repack nama karakter)
   Malie_Script_Tool.exe -s -in exec.dat -out exec_patched.dat -txt exec_strings.txt

4. Gunakan exec_patched.dat sebagai input untuk langkah berikutnya

5. Export dialog dari file yang sudah dipatch
   Malie_Script_Tool.exe -e -in exec_patched.dat -out exec_dialog.txt

6. Edit dialog di exec_dialog.txt

7. Import dialog (repack dialog)
   Malie_Script_Tool.exe -i -in exec_patched.dat -out exec_final.dat -txt exec_dialog.txt

8. Ganti exec.dat di folder game dengan exec_final.dat
```

---

## Alur Kerja Lengkap (End-to-End)

```
[File game asli]
       │
       ▼
[1] Ekstrak data.dat  →  folder berisi exec.dat, gambar, audio, dll.
       │
       ▼
[2] Patch nama karakter di exec.dat  (-a → edit → -s)
       │
       ▼
[3] Extract & edit dialog  (-e → edit → -i)
       │
       ▼
[4] Repack folder kembali jadi .dat baru  (menu [3] CLI)
       │
       ▼
[File game hasil patch]
```

---

## Bukti Pengujian

Toolkit ini diuji pada:

**Soushuu Senshinkan Gakuen: Hachimyoujin**  
相州戦神館學園 八命陣

| Item | Detail |
|---|---|
| Developer | light |
| Engine | FreeMalie |
| Pengujian | Ekstrak arsip, patch nama karakter, patch dialog |
| Status | Berhasil |

---

> 🎮 **Kajiri Kamui Kagura (神咒神威神楽 / KKK)** tidak diuji di toolkit ini — KKK memiliki **toolkit eksklusif** di repo terpisah karena keunikan struktur arsip dan script-nya.
> → **[MalieKit — KKK Exclusive Toolkit](https://github.com/Jannabie/KKK)**

**Screenshot hasil terjemahan:**

| Soushuu Senshinkan Gakuen: Hachimyoujin (相州戦神館學園 八命陣) |
|:---:|
| ![Translation working in-game](https://i.imgur.com/WLKLpyp.jpeg) |

---

## Catatan

- Repack `.dat` **terenkripsi** belum didukung — hasil repack plain `.dat` mungkin tidak terbaca oleh game tertentu yang menggunakan enkripsi penuh.
- File `namafile_entries.json` yang dihasilkan saat ekstrak **harus disimpan** dan digunakan saat repack agar struktur arsip tetap valid.
- Tool ini merupakan modifikasi dari [Malie_Script_Tool](https://github.com/crskycode/Malie_Script_Tool) oleh crskycode, dengan tambahan dukungan import strings (`-s`) untuk penggantian nama karakter.

---

## Lisensi

Proyek ini bersifat open-source untuk keperluan preservasi dan fan translation.  
Credit: crskycode (Malie_Script_Tool original), modifikasi & toolkit oleh kontributor proyek ini.
