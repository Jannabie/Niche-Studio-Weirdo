import sys
import os
import logging
from execution import unpack_plain, unpack, repack_plain, mgfpng_change

# Set base_dir berdasarkan lokasi eksekusi (Nuitka/normal)
if getattr(sys, 'frozen', False):
    base_dir = os.path.dirname(sys.executable)
else:
    base_dir = os.path.dirname(os.path.abspath(__file__))

if base_dir not in sys.path:
    sys.path.insert(0, base_dir)

# Pengaturan log
log_path = os.path.join(base_dir, "cli_runlog.txt")
logging.basicConfig(
    filename=log_path,
    level=logging.DEBUG,
    format="%(asctime)s [%(levelname)s] %(message)s"
)

def print_banner():
    banner = r"""
    ___  ___        _  _         _   _        ______       ______               _                
    |  \/  |       | |(_)       | | | |       | ___ \      | ___ \             | |               
    | .  . |  __ _ | | _   ___  | | | | _ __  | |_/ /  ___ | |_/ /  __ _   ___ | | __  ___  _ __ 
    | |\/| | / _` || || | / _ \ | | | || '_ \ |    /  / _ \|  __/  / _` | / __|| |/ / / _ \| '__|
    | |  | || (_| || || ||  __/ | |_| || | | || |\ \ |  __/| |    | (_| || (__ |   < |  __/| |   
    \_|  |_/ \__,_||_||_| \___|  \___/ |_| |_|\_| \_| \___|\_|     \__,_| \___||_|\_\ \___||_|   

  Alat CLI Malie Engine UnPacker / RePacker
  ※ Repack .lib belum dites. Repack terenkripsi .dat belum didukung karena belum terbaca oleh game. ※
  -------------------------------
    """
    print(banner)

def main():
    while True:
        print_banner()
        print("Pilih tugas yang ingin dijalankan:")
        print("  [1] Dekripsi tahap 1 (.dat saja)")
        print("  [2] Ekstrak penuh (.lib/.dat)")
        print("  [3] Repack plain (.dat saja)")
        print("  [4] Konversi MGF ↔ PNG")
        print("  [Q] Keluar")
        print("")

        choice = input("▶ Pilih nomor: ").strip().lower()
        logging.info(f"[pilihan] input pengguna: {choice}")

        if choice == "q":
            print("Program dihentikan.")
            break

        elif choice == "1":
            dat_path = input("Masukkan jalur .dat untuk dekripsi tahap 1 [q = batal]: ").strip('" ')
            if dat_path.lower() in ["q", "batal"]:
                continue

            out_dir = input("Masukkan jalur folder output (kosong = './output') [q = batal]: ").strip('" ')
            if out_dir.lower() in ["q", "batal"]:
                continue
            if not out_dir:
                out_dir = os.path.join(base_dir, "output")

            logging.info(f"[dekripsi tahap 1] dat_path: {dat_path}, out_dir: {out_dir}")
            unpack_plain.main([dat_path, out_dir])

        elif choice == "2":
            dat_path = input("Masukkan jalur .dat untuk diekstrak [q = batal]: ").strip('" ')
            if dat_path.lower() in ["q", "batal"]:
                continue

            out_dir = input("Masukkan jalur folder output (kosong = './output') [q = batal]: ").strip('" ')
            if out_dir.lower() in ["q", "batal"]:
                continue
            if not out_dir:
                out_dir = os.path.join(base_dir, "output")

            logging.info(f"[ekstrak penuh] dat_path: {dat_path}, out_dir: {out_dir}")
            unpack.main([dat_path, out_dir])

        elif choice == "3":
            input_dir = input("Masukkan jalur folder yang berisi file sumber [q = batal]: ").strip('" ')
            if input_dir.lower() in ["q", "batal"]:
                continue

            out_dat = input("Masukkan nama file .dat output [q = batal]: ").strip('" ')
            if out_dat.lower() in ["q", "batal"]:
                continue

            json_path = input("Masukkan jalur metadata .json [q = batal]: ").strip('" ')
            if json_path.lower() in ["q", "batal"]:
                continue

            logging.info(f"[repack plain] input_dir: {input_dir}, out_dat: {out_dat}, json_path: {json_path}")
            repack_plain.main([input_dir, out_dat, json_path])

        elif choice == "4":
            file_path = input("Masukkan jalur file .mgf atau .png yang akan dikonversi [q = batal]: ").strip('" ')
            if file_path.lower() in ["q", "batal"]:
                continue

            if not os.path.isfile(file_path):
                print(f"[Galat] File tidak ditemukan: {file_path}")
                continue

            ext = os.path.splitext(file_path)[1].lower()

            if ext == ".mgf":
                logging.info(f"[MGF→PNG] file: {file_path}")
                success = mgfpng_change.convert_mgf_to_png(file_path)
            elif ext == ".png":
                logging.info(f"[PNG→MGF] file: {file_path}")
                success = mgfpng_change.convert_png_to_mgf(file_path)
            else:
                logging.warning(f"[Galat] ekstensi tidak didukung: {ext}")
                print("❌ Ekstensi tidak didukung. Hanya .mgf atau .png yang diperbolehkan.")
                continue

            if not success:
                print("⚠️ Terjadi galat saat konversi. Periksa log.")

        else:
            logging.warning(f"[Peringatan] input salah: {choice}")
            print("Masukkan nomor yang benar.")
            continue

        while True:
            go_back = input("Pekerjaan selesai. Kembali ke menu utama? (Y/N): ").strip().lower()
            if go_back == "y":
                break
            elif go_back == "n":
                print("Program dihentikan.")
                return
            else:
                print("Masukkan Y atau N.")

if __name__ == "__main__":
    try:
        logging.info("==== mulai menjalankan ====")
        main()
        logging.info("==== selesai menjalankan ====")
    except Exception as e:
        logging.exception("Terjadi galat:")
        print("Terjadi galat tak terduga. Lihat 'cli_runlog.txt' untuk detailnya.")
