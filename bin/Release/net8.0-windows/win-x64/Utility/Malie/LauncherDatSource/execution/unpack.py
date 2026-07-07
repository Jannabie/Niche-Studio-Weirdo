# unpack.py - kode ekstrak penuh untuk mesin Malie
# otomatis membuat metadata.json yang diperlukan untuk repack.

import sys
import os
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

import io
from io import BytesIO
import logging
from logging.handlers import RotatingFileHandler
from pathlib import Path
from tqdm import tqdm
import time

from formats.fileview import FileView, FileFrame, FileStream
from formats.arccommon import AutoEntry, PrefixStream, NotTransform
from malie.malieunpack import DatOpener, LibOpener, read_encrypted
from malie.imagemgf import MgfFormat #말리 엔진 이미지 처리 코드
from malie.imagedzi import DziFormat #말리 엔진 이미지 처리 코드
from gameres.gameres import FormatCatalog
from gameres.audioogg import OggAudio, OggFormat

from gameres.imagepng import PngFormat 
from gameres.utility import TextSaver, BinarySaver, EntryMetadataManager


# pengaturan logger
logger = logging.getLogger()
logger.setLevel(logging.DEBUG)

file_format = logging.Formatter("[%(levelname)s] %(message)s")

# jalur penyimpanan folder
def ensure_dir(path):
    dir_path = os.path.dirname(path)
    if dir_path and not os.path.exists(dir_path):
        os.makedirs(dir_path, exist_ok=True)   

def run_serial_unpack(archive, view, output_dir):
    for i, entry in enumerate(tqdm(archive.entries, desc="Sedang dekripsi", unit="파일")):
        try:
            save_path = os.path.join(output_dir, entry.name)
            os.makedirs(os.path.dirname(save_path), exist_ok=True)

            print(f"[Sedang] {i+1}/{len(archive.entries)} → {entry.name}")  # ✅ 이제 i가 정의됨
            process_file(view, entry, save_path)
        except Exception as e:
            logging.error(f"[Galat - {entry.name}] {e}")


# 확장자 열기
def process_file(view, entry, save_path):
    try:
        if entry.is_dir:
            return

        ext = os.path.splitext(entry.name)[1].lower()

        # 포맷별 처리
        if ext == ".ogg":
            process_ogg_file(entry, save_path)

        elif ext in (".png", ".pn", ".mgf"):
            process_png_file(entry, save_path)

        elif ext == ".dzi":
            process_dzi_file(entry, save_path)

        elif ext == ".svg":
            process_svg_file(entry, save_path)

        elif ext in (".csv", ".txt", ".bat"):
            process_csv_file(entry, save_path)

        elif ext == ".mpg":
            process_mpg_file(entry, save_path)

        elif ext == ".swf":
            process_swf_file(entry, save_path)

        else:
            # 알 수 없는 확장자(.dat 포함)는 원본 바이트 그대로 저장
            # -> system/exec.dat 같은 엔트리가 여기서 누락되지 않게 함
            process_other_file(entry, save_path)

    except Exception as e:
        print(f"[unpack_test] {entry.name} terjadi galat saat memproses: {e}")

# FormatCatalog등록
FormatCatalog.add_format(OggFormat())
FormatCatalog.add_format(PngFormat())
FormatCatalog.add_format(MgfFormat())
FormatCatalog.add_format(DziFormat())

#ogg 파일 열람 처리
def process_ogg_file(entry, save_path):
    try:
        os.makedirs(os.path.dirname(save_path), exist_ok=True)

        stream = decrypt_ogg_stream(entry)
        stream.seek(0)

        handler = OggFormat()
        OggAudio = handler.try_open(stream)  # 디코딩 시도 → gagal해도 무시

        # 무조건 저장
        stream.seek(0)
        with open(save_path, "wb") as f:
            f.write(stream.read())

        logging.debug(f"{entry.name} → dekripsi + simpan selesai (.ogg)")

    except Exception as e:
        logging.error(f"{entry.name} terjadi galat saat memproses (.ogg): {e}")

        
#Png(+mgf) 파일 열람 처리
def process_png_file(entry, save_path_base):
    try:
        os.makedirs(os.path.dirname(save_path_base), exist_ok=True)

        ext = Path(entry.name).suffix.lower()
        is_mgf = ext == ".mgf"
        is_pn = ext == ".pn"

        # 🔹 MGF일 경우: .mgf 원본만 저장
        if is_mgf:
            stream = decrypt_mgf_stream(entry)
            if stream:
                mgf_path = str(Path(save_path_base).with_suffix(".mgf"))
                with open(mgf_path, "wb") as f:
                    f.write(stream.read())
                logging.debug(f"{entry.name} → MGF 원본 simpan selesai: {mgf_path}")
            else:
                logging.warning(f"{entry.name} → decrypt_mgf_stream gagal")
            return  # ✅ PNG 변환 생략하고 여기서 종료

        # 🔸 PNG 또는 .pn 처리
        image = decrypt_png_stream(entry)
        if not image:
            logging.warning(f"{entry.name} → decrypt_png_stream gagal")
            return

        png_path = str(Path(save_path_base).with_suffix(".png"))
        with open(png_path, "wb") as f:
            stream = BytesIO()
            PngFormat().write(stream, image)
            f.write(stream.getvalue())

        if is_pn:
            logging.debug(f"{entry.name} → PNG 변환 simpan selesai (from .pn): {png_path}")
        else:
            logging.debug(f"{entry.name} → PNG simpan selesai: {png_path}")

    except Exception as e:
        logging.error(f"{entry.name} → terjadi galat: {e}")

# Dzi 파일 열람 처리
def process_dzi_file(entry, save_path):
    try:
        os.makedirs(os.path.dirname(save_path), exist_ok=True)

        stream = decrypt_dzi_stream(entry)  # ✅ key_name 전달 제거
        if not stream:
            logging.warning(f"{entry.name} → decrypt_dzi_stream gagal")
            return

        with open(save_path, "w", encoding="utf-8") as f:
            stream.seek(0)
            f.write(stream.read().decode("utf-8"))

        logging.debug(f"{entry.name} → dekripsi + simpan selesai (.dzi)")

    except Exception as e:
        logging.error(f"{entry.name} terjadi galat saat memproses (.dzi): {e}")

#svg 파일 열람 처리
def process_svg_file(entry, save_path):
    try:
        os.makedirs(os.path.dirname(save_path), exist_ok=True)

        stream = decrypt_svg_stream(entry) 
        if not stream:
            logging.warning(f"{entry.name} → decrypt_svg_stream gagal")
            return

        raw_data = stream.read()
        TextSaver.save_file(entry.name, raw_data, save_path)

    except Exception as e:
        logging.error(f"[오류 - svg] {entry.name} terjadi galat saat memproses: {e}")

#csv 파일 열람 처리
def process_csv_file(entry, save_path):
    try:
        os.makedirs(os.path.dirname(save_path), exist_ok=True)

        stream = decrypt_csv_stream(entry)
        if not stream:
            logging.warning(f"{entry.name} → decrypt_csv_stream gagal")
            return

        raw_data = stream.read()
        TextSaver.save_file(entry.name, raw_data, save_path)

    except Exception as e:
        logging.error(f"[오류 - csv] {entry.name} terjadi galat saat memproses: {e}")

#mpg 파일 열람 처리
def process_mpg_file(entry, save_path):
    try:
        os.makedirs(os.path.dirname(save_path), exist_ok=True)

        stream = decrypt_mpg_stream(entry)
        if not stream:
            logging.warning(f"{entry.name} → decrypt_mpg_stream gagal")
            return

        raw_data = stream.read()
        BinarySaver.save(entry.name, raw_data, save_path)

    except Exception as e:
        logging.error(f"[오류 - other] {entry.name} terjadi galat saat memproses: {e}")
        
#swf 파일 열람 처리 - 1차 암호화만 Camellia, 2차 언팩은 
def process_swf_file(entry, save_path):
    try:
        os.makedirs(os.path.dirname(save_path), exist_ok=True)

        stream = decrypt_swf_stream(entry)  # ← 이미 복호화된 BytesIO 반환
        if not stream:
            logging.warning(f"{entry.name} → SWF 복호화 gagal")
            return

        with open(save_path, "wb") as f:
            stream.seek(0)
            f.write(stream.read())

        logging.debug(f"{entry.name} → dekripsi + simpan selesai (.swf)")

    except Exception as e:
        logging.error(f"{entry.name} terjadi galat saat memproses (.swf): {e}")

#기타 파일들 처리(.psd 같은거)
def process_other_file(entry, save_path):
    try:
        os.makedirs(os.path.dirname(save_path), exist_ok=True)
        stream = decrypt_other_stream(entry)
        if not stream:
            logging.warning(f"{entry.name} → decrypt_other_stream gagal")
            return

        raw_data = stream.read()
        BinarySaver.save(entry.name, raw_data, save_path)

    except Exception as e:
        logging.error(f"[오류 - other] {entry.name} terjadi galat saat memproses: {e}")

#OGG 복호화 로직
def decrypt_ogg_stream(entry):
    view = entry.archive.file_view
    decryptor = entry.archive.decryptor
    offset = entry.offset
    size = entry.size

    buf = bytearray(size)
    read_encrypted(view, decryptor, offset, buf, 0, size)
    return BytesIO(buf)
    
#PNG/MGF 분기 로직
def decrypt_png_stream(entry):
    try:
        view = entry.archive.file_view
        decryptor = entry.archive.decryptor
        offset = entry.offset
        size = entry.size

        buf = bytearray(size)
        read_encrypted(view, decryptor, offset, buf, 0, size)
        prefix = buf[:8]
        logging.debug(f"[decrypt_png_stream] {entry.name}에서 읽은 크기 = {len(buf)}")

        # PNG 시그니처 감지되면 처리
        if prefix.startswith(b'\x89PNG\r\n\x1a\n'):
            return decrypt_png_normal(entry, buf)

        # MGF 시그니처는 더 이상 여기서 처리하지 않음
        if prefix.startswith(b'MalieGF'):
            logging.debug(f"[decrypt_png_stream] {entry.name} → MGF 시그니처 감지, PNG 처리 생략")
            return None

        logging.warning(f"[decrypt_png_stream] {entry.name} → signature tidak dikenal: {prefix}")
        return None

    except Exception as e:
        logging.error(f"[decrypt_png_stream] terjadi galat: {e}", exc_info=True)
        return None
    
#시그니처 감지 후 PNG일 경우 PNG로 처리
def decrypt_png_normal(entry, data):
    try:
        stream = BytesIO(data)

        # ✅ 확장자 없을 때 대비
        if entry.name and not hasattr(stream, "name"):
            stream.name = entry.name

        sig = stream.read(8)
        stream.seek(0)
        if sig != b'\x89PNG\r\n\x1a\n':
            logging.warning(f"[decrypt_png_normal] PNG signature tidak cocok: {sig}")
            return None

        handler = PngFormat()
        metadata = handler.read_metadata(stream)
        if not metadata:
            logging.warning(f"[decrypt_png_normal] read_metadata gagal: {entry.name}")
            return None

        logging.debug(f"[decrypt_png_normal] read 호출 전: metadata = {metadata}, stream size = {len(data)}")
        result = handler.read(stream, metadata)
        logging.debug(f"[decrypt_png_normal] read 호출 완료")
        return result

    except Exception as e:
        logging.error(f"[decrypt_png_normal] terjadi galat: {e}", exc_info=True)
        return None
    
#MGF 원본으로 저장하고 싶으면 이쪽으로 
def decrypt_mgf_stream(entry) -> BytesIO | None:
    try:
        view = entry.archive.file_view
        decryptor = entry.archive.decryptor
        offset = entry.offset
        size = entry.size

        buf = bytearray(size)
        read_encrypted(view, decryptor, offset, buf, 0, size)

        if not buf.startswith(b'MalieGF'):
            logging.warning(f"[decrypt_mgf_stream] {entry.name} → signature tidak cocok")
            return None

        return BytesIO(buf)

    except Exception as e:
        logging.error(f"[decrypt_mgf_stream] {entry.name} terjadi galat: {e}")
        return None

#dzi 복호화 로직
def decrypt_dzi_stream(entry):
    try:
        view = entry.archive.file_view
        decryptor = entry.archive.decryptor
        offset = entry.offset
        size = entry.size

        buf = bytearray(size)
        read_encrypted(view, decryptor, offset, buf, 0, size)
        stream = BytesIO(buf)

        # ✅ DziFormat 사용해 메타데이터만 검사 (png ekstraksi 아님)
        fmt = DziFormat()
        metadata = fmt.read_metadata(stream)
        if not metadata:
            logging.warning("[decrypt_dzi_stream] DZI 메타데이터 읽기 gagal")
            return None

        stream.seek(0)
        return stream

    except Exception as e:
        logging.error(f"[decrypt_dzi_stream] DZI terjadi galat saat memproses: {e}")
        return None

#svg 복호화 로직
def decrypt_svg_stream(entry) -> io.BytesIO | None:
    try:
        view = entry.archive.file_view
        decryptor = entry.archive.decryptor
        offset = entry.offset
        size = entry.size

        buf = bytearray(size)
        read_encrypted(view, decryptor, offset, buf, 0, size)

        stream = io.BytesIO(buf)
        stream.seek(0)

        logging.debug(f"[성공 - decrypt_svg_stream] SVG dekripsi berhasil")
        return stream
    except Exception as e:
        logging.error(f"[오류 - decrypt_svg_stream] SVG terjadi galat saat memproses: {e}")
        return None
    
#csv 복호화 로직
def decrypt_csv_stream(entry) -> io.BytesIO | None:
    try:
        if entry.size < 16:
            logging.debug(f"[arccommon 적용] berkas CSV kecil (size={entry.size}) → NotTransform() 사용")

            # 🔒 반드시 새로 BytesIO 생성
            try:
                raw_data = entry.archive.open_entry(entry)
                if not raw_data or all(b == 0 for b in raw_data):
                    logging.warning(f"[decrypt_csv_stream] {entry.name} → isinya seluruhnya 0x00")
                    return None
                
                transformer = NotTransform()
                raw = transformer.transform_block(raw_data)
                return io.BytesIO(raw)

            except Exception as e:
                logging.warning(f"[decrypt_csv_stream] {entry.name} → raw_stream 생성 gagal: {e}")
                return None

        # 📌 일반적인 암호화된 CSV 처리
        view = entry.archive.file_view
        decryptor = entry.archive.decryptor
        offset = entry.offset
        size = entry.size

        buf = bytearray(size)
        read_encrypted(view, decryptor, offset, buf, 0, size)

        return io.BytesIO(buf)

    except Exception as e:
        logging.warning(f"[decrypt_csv_stream] {entry.name} → terjadi galat: {e}")
        return None
    
#mpg 복호화 로직 
def decrypt_mpg_stream(entry) -> io.BytesIO | None:
    try:
        view = entry.archive.file_view
        decryptor = entry.archive.decryptor
        offset = entry.offset
        size = entry.size

        logging.debug(f"[decrypt_mpg_stream] 시작: {entry.name} offset=0x{offset:X}, size={size}")

        buf = bytearray(size)
        logging.debug(f"[decrypt_mpg_stream] read_encrypted 호출 전")

        read_encrypted(view, decryptor, offset, buf, 0, size)

        logging.debug(f"[decrypt_mpg_stream] read_encrypted 호출 후")

        stream = io.BytesIO(buf)
        stream.seek(0)

        logging.debug(f"[성공 - decrypt_mpg_stream] MPG dekripsi berhasil")
        return stream
    except Exception as e:
        logging.error(f"[오류 - decrypt_mpg_stream] MPG terjadi galat saat memproses: {e}")
        return None
    
#swf 복호화 로직
def decrypt_swf_stream(entry) -> io.BytesIO | None:
    try:
        view = entry.archive.file_view
        decryptor = entry.archive.decryptor
        offset = entry.offset
        size = entry.size

        buf = bytearray(size)
        read_encrypted(view, decryptor, offset, buf, 0, size)
        stream = io.BytesIO(buf)
        stream.seek(0)

        # 시그니처 검사
        sig = stream.read(3)
        if sig not in (b"CWS", b"FWS", b"ZWS"):
            logging.warning(f"[decrypt_swf_stream] SWF signature tidak cocok: {sig.hex()}")
            return None

        stream.seek(0)
        return stream

    except Exception as e:
        logging.error(f"[decrypt_swf_stream] terjadi galat: {e}")
        return None
    
#기타 파일들 복호화 로직(.psd, 확장자 없는 일부 파일도 대응.)
def decrypt_other_stream(entry) -> io.BytesIO | None:
    try:
        view = entry.archive.file_view
        decryptor = entry.archive.decryptor
        offset = entry.offset
        size = entry.size

        buf = bytearray(size)
        read_encrypted(view, decryptor, offset, buf, 0, size)

        sig = buf[:512]  # svg는 앞부분 넉넉하게 봐도 좋음
        preview = sig.decode("utf-8", errors="ignore")

        # 확장자 없음 + <svg 감지> 전용 처리
        if b"<svg" in sig or preview.lstrip().startswith("<svg"):
            logging.debug(f"[detect] SVG 감지 → svg 처리")
            return decrypt_svg_stream(entry)

        # 기타는 BytesIO로 반환
        return io.BytesIO(buf)

    except Exception as e:
        logging.error(f"[decrypt_other_stream] {entry.name} terjadi galat: {e}")
        return None


def main(args=None):
    if args is None:
        args = sys.argv[1:]

    try:
        if len(args) < 1:
            print("Cara pakai: ketik 2 lalu <input.dat>/<output.dir>")
            return

        input_path = args[0]
        output_dir = os.path.splitext(input_path)[0]
        os.makedirs(output_dir, exist_ok=True)

        view = FileView(input_path)

        archive = LibOpener().try_open(view)
        if archive:
            logging.debug("[unpack] LibOpener berhasil")
        else:
            logging.debug("[unpack] LibOpener gagal → DatOpener 시도")
            view.close()
            view = FileView(input_path)
            archive = DatOpener().try_open(view)
            if archive:
                print(f"[DatOpener] jumlah entri: {len(archive.entries)}")
                logging.debug("[unpack] DatOpener berhasil")
            else:
                logging.error("[unpack] DatOpener gagal → 아카이브 열기 gagal")
                return

        # — 여기서 JSON 메타데이터 적용 시작 —
        json_path = os.path.splitext(input_path)[0] + "_entries.json"
        for entry in archive.entries:
            entry.source_archive = os.path.basename(input_path)  # 예: "sound.dat"

        meta_manager = EntryMetadataManager(json_path)
        meta_manager.assign_order(archive.entries)
        meta_manager.update_padding(archive.entries, view.size, base_offset=archive.base_offset)

        if os.path.isfile(json_path):
            logging.info(f"[unpack_test] memuat metadata JSON: {json_path}")
            meta_manager.apply_to_entries(archive.entries)
        else:
            logging.info(f"[unpack_test] JSON 메타데이터 berkas tidak ada, membuat baru: {json_path}")

        # 항상 JSON 메타데이터 저장 (새로 생성 또는 덮어쓰기)
        meta_manager.save_metadata(archive.entries)

        # ✅ waktu dekripsi 측정 시작
        start = time.time()
        run_serial_unpack(archive, view, output_dir)
        elapsed = time.time() - start
        print(f"[완료] 전체 waktu dekripsi: {elapsed:.2f}초")


    except KeyboardInterrupt:
        print("\n[batal] dihentikan oleh pengguna.")
        logging.warning("[main] 사용자 중단 (Ctrl+C)")
        os._exit(1)

    except Exception as e:
        logging.exception(f"[main] terjadi galat: {e}")

    finally:
        try:
            view.close()
            print("[unpack_test] view ditutup")
        except Exception:
            pass
        print("[main] selesai menjalankan")
        logging.shutdown()


if __name__ == "__main__":
    print("[main] mulai menjalankan")
    main()
    print("[main] selesai menjalankan")  # 이게 안 뜨면 종료 안 되고 재진입
