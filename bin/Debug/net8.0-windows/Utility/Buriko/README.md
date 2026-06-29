# BGI Translator

Aplikasi editor terjemahan untuk visual novel berbasis engine **Ethornell / BGI (Buriko General Interpreter)**.
Diuji pada: **Sakura no Uta -Haru no Yuki-** (さくらのうた -桜の詩-)

---

## Apa Ini?

BGI Translator adalah aplikasi Windows dengan antarmuka grafis yang memudahkan proses penerjemahan file script (`.sc`) dari game visual novel berbasis engine Ethornell. Aplikasi ini dibangun di atas library `EthornellEditor.dll` karya **arcusmaximus**, dengan tambahan fitur yang tidak ada di EEGUI (editor original dari arcusmaximus) seperti pencarian teks, filter baris, glosarium batch, progress bar, ekspor/impor TSV, dan dark theme.

| Fitur | EEGUI | BGI Translator |
|---|---|---|
| Editor dua kolom JP ↔ ID | ✗ | ✓ |
| Pencarian teks realtime | ✗ | ✓ |
| Filter baris belum diterjemahkan | ✗ | ✓ |
| Deteksi dan sisip tag otomatis | ✗ | ✓ |
| Progress bar | ✗ | ✓ |
| Ekspor / Impor TSV | ✗ | ✓ |
| Glosarium otomatis batch | ✗ | ✓ |
| Drag & drop file | ✗ | ✓ |
| Dark theme | ✗ | ✓ |

---

## Cara Pakai (Pengguna Biasa)

Jika hanya ingin langsung menggunakan aplikasinya tanpa perlu build dari source, ikuti langkah berikut.

### Tahap 1 — Download dan Setup

Download **[versi terbaru dari halaman Releases](https://github.com/Jannabie/BGI-Translator/releases)**. Ekstrak hasilnya ke sebuah folder, lalu pastikan isi folder tersebut mengandung file-file berikut sebelum dijalankan:

| File | Keterangan |
|---|---|
| `BGITranslator.exe` | Aplikasi utama (hasil download) |
| `EthornellEditor.dll` | **Wajib** — unduh dari [repo arcusmaximus](https://github.com/arcusmaximus/EthornellTools), letakkan di folder yang sama |
| `CSystemArc.exe` | Untuk ekstrak dan pack ulang arsip `.arc` (dari repo yang sama) |
| `BgiDisassembler.exe` | Untuk disassemble file script `.sc` — opsional |
| `BgiImageEncoder.exe` | Untuk konversi gambar — opsional |

`EthornellEditor.dll` **harus berada di folder yang sama** dengan `BGITranslator.exe`. Tanpa file ini, aplikasi tidak bisa membuka file script apapun.

### Tahap 2 — Ekstrak File Script dari Arsip Game

File script game tersimpan di dalam arsip berekstensi `.arc`. Ekstrak isinya menggunakan `CSystemArc.exe`:

```bash
CSystemArc.exe extract namagame.arc folder_output\
```

Hasilnya adalah folder berisi file-file `.sc` (script dialog) beserta aset-aset lain milik game.

### Tahap 3 — Terjemahkan dengan BGI Translator

Jalankan `BGITranslator.exe`, lalu buka file `.sc` yang ingin diterjemahkan lewat menu **File → Buka Script** atau cukup drag & drop file-nya langsung ke jendela aplikasi.

Teks asli bahasa Jepang akan muncul di kolom kiri, dan kolom kanan adalah tempat mengetik terjemahan. Beberapa fitur yang bisa dimanfaatkan untuk mempercepat pekerjaan:

**Pencarian** — Ketik kata kunci di kotak 🔍 untuk mencari teks, navigasi antar hasil dengan `F3` dan `Shift+F3`.

**Filter** — Pilih mode "Belum diterjemahkan" untuk menampilkan hanya baris yang kolom terjemahannya masih kosong, sehingga tidak perlu scroll manual mencari bagian yang belum selesai.

**Tag otomatis** — Tag-tag khusus seperti `\n` (baris baru) dan `@name` (nama karakter) terdeteksi secara otomatis dan bisa disisipkan dengan satu klik tanpa perlu mengetik manual.

**Glosarium batch** — Buka menu **Tools → Glosarium Otomatis**, masukkan pasangan istilah Jepang–Indonesia, lalu klik Terapkan. Semua kemunculan istilah tersebut di seluruh file akan diganti sekaligus.

**Ekspor/Impor TSV** — Terjemahan bisa diekspor ke file `.tsv` untuk backup atau dikerjakan bersama orang lain, lalu diimpor kembali setelah selesai lewat menu **File → Export/Import TSV**.

Setelah selesai, simpan dengan **File → Simpan**.

### Tahap 4 — Pack Ulang ke Arsip

Setelah semua script selesai diterjemahkan, pack kembali isi folder menjadi arsip `.arc`:

```bash
CSystemArc.exe pack folder_output\ namagame_patched.arc
```

Ganti file `.arc` asli di folder instalasi game dengan file yang sudah dipatch, lalu jalankan game-nya.

---

## Pintasan Keyboard

| Tombol | Aksi |
|---|---|
| `Ctrl+O` | Buka file script |
| `Ctrl+S` | Simpan |
| `Ctrl+F` | Fokus ke kotak pencarian |
| `F3` | Lompat ke hasil pencarian berikutnya |
| `Shift+F3` | Lompat ke hasil pencarian sebelumnya |
| `Ctrl+G` | Pergi ke nomor baris tertentu |
| `Enter` | Masuk ke mode edit sel terjemahan |
| `Tab` | Konfirmasi dan pindah ke baris berikutnya |
| `Esc` | Bersihkan pencarian |

---

## Format File TSV

File TSV yang diekspor menggunakan encoding UTF-8 dengan pemisah tab, dan formatnya seperti ini:

```
# BGI Translator Export | 2025-01-01 12:00
# Index	Original	Terjemahan
0	桜の森で	Di hutan sakura
1	世界が鳴った。	Dunia pun berdentang.
```

Format ini kompatibel dan bisa dibuka langsung di spreadsheet editor seperti LibreOffice Calc atau Google Sheets untuk dikerjakan bersama tim.

---

## Build dari Source (Untuk Developer)

Jika ingin mengkompilasi sendiri dari source code, berikut caranya.

**Persyaratan:** Windows 10/11 (64-bit), [.NET 10 SDK](https://dotnet.microsoft.com/download), dan `EthornellEditor.dll` yang diletakkan di folder yang sama dengan file `.csproj`.

```bash
git clone https://github.com/Jannabie/BGI-Translator.git
cd BGI-Translator
dotnet build -c Release
```

File hasil build akan berada di `bin\Release\net10.0-windows\BGITranslator.exe`.

---

## Kredit

Seluruh kemampuan membaca dan menulis format script BGI berasal dari library **`EthornellEditor.dll`** karya [arcusmaximus](https://github.com/arcusmaximus). Tool-tool pendukung seperti `CSystemArc.exe`, `BgiDisassembler.exe`, dan `BgiImageEncoder.exe` juga berasal dari repo yang sama. BGI Translator dibangun di atasnya sebagai lapisan antarmuka yang lebih lengkap.

---

## Disclaimer

Tool ini dibuat untuk keperluan edukasi dan penerjemahan personal. Pengguna bertanggung jawab penuh untuk memastikan penggunaannya sesuai dengan aturan copyright dan Terms of Service dari game yang bersangkutan.
