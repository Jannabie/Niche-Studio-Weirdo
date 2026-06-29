# unpack_plain.py - untuk mengekstrak .dat yang baru didekripsi tahap 1. 

import sys, os, io
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

import logging
import time

from formats.fileview import FileView
from malie.malieunpack import DatOpener, read_encrypted

# 로거 설정
logger = logging.getLogger()
logger.setLevel(logging.DEBUG)

file_format = logging.Formatter("[%(levelname)s] %(message)s")


# dekripsi penuh .dat + termasuk tabel string
def decrypt_full_dat(archive, output_path: str):
    view = archive.file_view
    decryptor = archive.decryptor
    file_size = view.get_max_offset()
    block_size = 0x1000

    with open(output_path, "wb") as out:
        offset = 0
        while offset < file_size:
            to_read = min(block_size, file_size - offset)
            # 항상 블록 정렬 유지 (원본 그대로)
            aligned_len = (to_read + 0xF) & ~0xF
            buf = bytearray(aligned_len)
            read_encrypted(view, decryptor, offset, buf, 0, aligned_len)
            out.write(buf)
            offset += aligned_len

        out.flush()
        os.fsync(out.fileno())

    print(f"[Selesai] Dekripsi tahap 1: {output_path}")
      

def main(args=None):
    if args is None:
        args = sys.argv[1:]

    try:
        if len(args) < 1:
            print("Cara pakai: ketik 1 lalu <input.dat>/<output.dir>")
            return

        input_path = args[0]

        if not os.path.isfile(input_path):
            print(f"[Galat] File tidak ditemukan: {input_path}")
            return

        view = FileView(input_path)
        archive = DatOpener().try_open(view)
        if not archive:
            print("Gagal membuka arsip")
            return

        # ✅ 항상 _plain.dat로 출력
        output_path = input_path.replace(".dat", "_plain.dat")

        start = time.time()
        decrypt_full_dat(archive, output_path)
        elapsed = time.time() - start
        print(f"[Selesai] Waktu dekripsi tahap 1: {elapsed:.2f} detik")
        print(f"[Selesai] File hasil dekripsi: {output_path}")

    except KeyboardInterrupt:
        print("\n[Batal] Dihentikan oleh pengguna.")
        logging.warning("[main] Dihentikan oleh pengguna (Ctrl+C)")
        os._exit(1)

    except Exception as e:
        logging.exception(f"[main] terjadi galat: {e}")

    finally:
        try:
            view.close()
            print("[main] FileView ditutup")
        except Exception:
            pass
        print("[main] selesai menjalankan")
        logging.shutdown()

if __name__ == "__main__":
    print("[main] mulai menjalankan")
    main()
    print("[main] selesai menjalankan")
