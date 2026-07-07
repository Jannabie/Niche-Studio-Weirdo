# repack_plain.py - untuk repack plain

import sys, os, struct, logging, argparse
from logging.handlers import RotatingFileHandler
from malie.malierepack import DatWriterplain
from gameres.utility import EntryMetadataApplier

# 로거 설정
logger = logging.getLogger()
logger.setLevel(logging.DEBUG)

file_format = logging.Formatter("[%(levelname)s] %(message)s")

def main(args=None):
    if args is None:
        args = sys.argv[1:]

    view = None
    try:
        # ✅ entry_meta.json을 필수로 받도록 3개 인자로 고정
        if len(args) != 3:
            print("Cara pakai: ketik 3 lalu <input_dir> <output_dat> <entry_meta.json>")
            sys.exit(1)

        input_dir = args[0]
        output_dat = args[1]
        json_path = args[2]  # ✅ 무조건 필요

        # ✅ output_dat가 상대경로이고 디렉토리 명시가 없을 경우 → input_dir 상위 디렉토리에 저장
        if not os.path.isabs(output_dat) and not os.path.dirname(output_dat):
            parent_dir = os.path.dirname(os.path.abspath(input_dir))
            output_dat = os.path.join(parent_dir, output_dat)
            logging.debug(f"[main] Jalur relatif disesuaikan (berdasarkan folder induk): {output_dat}")

        logging.debug(f"[main] Folder input: {input_dir}")
        logging.debug(f"[main] File output: {output_dat}")
        logging.debug(f"[main] Metadata JSON: {json_path}")

        # JSON이 실제로 존재하는지 체크
        if not os.path.isfile(json_path):
            print(f"[Galat] File metadata JSON tidak ditemukan: {json_path}")
            sys.exit(1)

        # ✅ 로그로 확인용 출력
        logging.info(f"[main] File DAT output akan disimpan di sini → {output_dat}")

        writer = DatWriterplain(
            entry_list=[],
            base_dir=input_dir
        )

        logging.debug("[main] mulai add_auto")
        writer.add_auto(input_dir, "", root_dir=input_dir)
        logging.debug(f"[main] jumlah entry terdaftar: {len(writer.entries)}")

        if json_path:
            applier = EntryMetadataApplier(json_path)
            applier.apply_to_entries(writer.entries)
            applier.apply_order(writer.entries)

        for e in writer.entries[:20]:
            logging.debug(f"[DEBUG-terapkan entry] {e.get('arc_path')} | entry_index={e.get('entry_index')}")

        writer.finalize_folders()

        writer.write.write_header()
        writer.write.write_index_table()
        writer.write.calculate_base_offset()
        writer.write.write_data()
        writer.write.prepare_offsets()
        writer.write.write_offset_table()

        logging.debug("[main] mulai menyimpan file")
        writer.save.to_file(output_dat)
        logging.info(f"[Selesai] Repack berhasil → {output_dat}")

    except KeyboardInterrupt:
        print("\n[Batal] Dihentikan oleh pengguna.")
        logging.warning("[main] Dihentikan oleh pengguna (Ctrl+C)")
        os._exit(1)

    except Exception as e:
        logging.exception(f"[Galat] terjadi pengecualian: {e}")

    finally:
        try:
            if view:
                view.close()
                print("[repack_test] view ditutup")
        except Exception:
            pass
        print("[main] selesai menjalankan")
        logging.shutdown()


if __name__ == "__main__":
    print("[main] mulai menjalankan")
    main()
    print("[main] selesai menjalankan")