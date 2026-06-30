import os
import sys
import argparse
import logging
from io import BytesIO
from formats.fileview import FileView
from malie.imagemgf import MgfFormat
from gameres.imagepng import PngFormat

def setup_logger():
    # 기존 핸들러 제거
    root = logging.getLogger()
    if root.handlers:
        for handler in root.handlers[:]:
            root.removeHandler(handler)

    logging.basicConfig(
        level=logging.DEBUG,  # 디버그 로그 출력
        format="%(message)s",
        handlers=[logging.StreamHandler(sys.stdout)]
    )

def convert_mgf_to_png(mgf_path):
    base, _ = os.path.splitext(mgf_path)
    png_output_path = base + ".png"

    logging.info(f"[MGF→PNG] {mgf_path} → {png_output_path}")

    try:
        view = FileView(mgf_path)
        logging.debug(f"[debug] FileView berhasil dibuka: size={view.size}")

        data = view.read(0, view.size)
        logging.debug(f"[debug] berhasil membaca seluruh data file (bytes={len(data)})")

        stream = BytesIO(data)
        handler = MgfFormat()

        logging.debug("[debug] memanggil read_metadata()")
        info = handler.read_metadata(stream)
        if not info:
            raise ValueError("read_metadata gagal")
        logging.debug(f"[debug] metadata berhasil diekstrak: {info}")

        stream.seek(0)
        logging.debug("[debug] memanggil read()")
        image = handler.read(stream, info)
        logging.debug(f"[debug] objek gambar berhasil dipulihkan: {type(image)}")

        logging.debug("[debug] mulai menyimpan PNG")
        with open(png_output_path, "wb") as f:
            PngFormat().write(f, image)

        logging.info(f"[Selesai] tersimpan: {png_output_path}")
        return True

    except Exception as e:
        logging.error(f"[Galat] konversi MGF→PNG gagal: {e}", exc_info=True)
        return False

def convert_png_to_mgf(png_path: str):
    logging.info(f"[PNG→MGF] {png_path} → {os.path.splitext(png_path)[0]}.mgf")

    try:
        # FileView로 PNG 파일 열기
        view = FileView(png_path)
        logging.debug(f"[debug] FileView berhasil dibuka: size={view.size}")

        # PNG 데이터 읽기
        data = view.read(0, view.size)
        stream = BytesIO(data)

        handler = PngFormat()
        logging.debug("[debug] memanggil read_metadata()")
        info = handler.read_metadata(stream)
        if not info:
            raise ValueError("read_metadata gagal")

        logging.debug("[debug] memanggil read()")
        stream.seek(0)
        image = handler.read(stream, info)
        if image is None:
            raise ValueError("이미지 디코딩 실패")

        # MGF로 저장
        output_path = os.path.splitext(png_path)[0] + ".mgf"
        logging.debug(f"[debug] mulai menyimpan MGF → {output_path}")
        with open(output_path, "wb") as f:
            MgfFormat().write(f, image)

        logging.info(f"[Selesai] konversi PNG→MGF berhasil: {output_path}")
        return True

    except Exception as e:
        logging.error(f"[Galat] konversi PNG→MGF gagal: {e}", exc_info=True)
        return False



def main():
    setup_logger()

    parser = argparse.ArgumentParser(description="Konversi antara format MGF dan PNG.")
    parser.add_argument("input", help="Input file (.mgf or .png)")
    parser.add_argument("--to-png", action="store_true", help="Konversi MGF ke PNG")
    parser.add_argument("--to-mgf", action="store_true", help="Konversi PNG ke MGF")
    parser.add_argument("--verbose", action="store_true", help="Enable debug logging")
    args = parser.parse_args()

    if args.verbose:
        logging.getLogger().setLevel(logging.DEBUG)

    if args.to_png:
        success = convert_mgf_to_png(args.input)
        sys.exit(0 if success else 1)
    elif args.to_mgf:
        success = convert_png_to_mgf(args.input)
        sys.exit(0 if success else 1)
    else:
        logging.error("❌ Tentukan arah konversi: --to-png atau --to-mgf")
        sys.exit(1)

if __name__ == "__main__":
    main()
