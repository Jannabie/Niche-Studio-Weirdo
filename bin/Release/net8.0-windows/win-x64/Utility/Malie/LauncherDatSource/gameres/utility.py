# utility.py - Camellia cipher decryptor used in GARbro's Malie engine handler
# Ported from C# by morkt (GARbro: https://github.com/morkt/GARbro)

# MIT License (for GARbro ported structure)
# Copyright (c) morkt

# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to
# deal in the Software without restriction, including without limitation the
# rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
# sell copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:

# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.

# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
# FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
# IN THE SOFTWARE.

# GARbro(by. morkt) 1.1.6 ver.을 기준으로 Python으로 포팅했습니다.
# Python에 맞춰 전용 헬퍼 함수들도 추가함.
# 디렉토리 정보 건으로 도움 및 힌트를 주신 Neidhardt님께 감사드립니다.


import zlib, os, struct, json, subprocess, cv2, mimetypes
import logging
from typing import Optional
from mutagen.oggvorbis import OggVorbis
from mutagen import MutagenError
from PIL import Image

# ============================
# Encodings
# ============================
try:
    SHIFT_JIS = 'cp932'
except Exception as ex:
    print(f"[Galat] Inisialisasi Encodings gagal: {ex}")

# ============================
# Endian Utilities
# ============================
class BigEndian:
    @staticmethod
    def ToUInt32(buf, index):
        return int.from_bytes(buf[index:index+4], 'big')

    @staticmethod
    def ToInt32(buf, index):
        return int.from_bytes(buf[index:index+4], 'big', signed=True)
    
    @staticmethod
    def ToInt32_bytes(val):
        return val.to_bytes(4, 'big', signed=True)

    @staticmethod
    def ToUInt32_bytes(val):
        return val.to_bytes(4, 'big')

class LittleEndian:
    @staticmethod
    def ToUInt16(buf, index):
        return int.from_bytes(buf[index:index+2], 'little')
    
    @staticmethod
    def ToInt16(buf, index):
        return int.from_bytes(buf[index:index+2], 'little', signed=True)
    @staticmethod
    def ToUInt32(buf, index):
        return int.from_bytes(buf[index:index+4], 'little')

    @staticmethod
    def ToInt32(buf, index):
        return int.from_bytes(buf[index:index+4], 'little', signed=True)

    @staticmethod
    def PackToBuf(buf, index, val):
        buf[index:index+4] = val.to_bytes(4, 'little')

    # 원본 GARbro에는 없음. Python 포팅 전용 헬퍼. PackToBuf과 유사하지만, 직접 byte[]를 반환함.
    @staticmethod
    def GetBytes32(val: int) -> bytes:
        return val.to_bytes(4, 'little')

# ============================
# ASCII / Binary helpers
# ============================
def ascii_equal(buf: bytes, offset: int, s: str | bytes) -> bool:
    if isinstance(s, str):
        s = s.encode('ascii')
    return buf[offset:offset+len(s)] == s

def get_cstring(buf: bytes, index: int, limit: int, encoding=SHIFT_JIS) -> str:
    end = index
    while end < index + limit and buf[end] != 0:
        end += 1
    try:
        return buf[index:end].decode(encoding)
    except:
        return ''

def get_cstring_default(buf: bytes, index: int, limit: int) -> str:
    return get_cstring(buf, index, limit, encoding=SHIFT_JIS)

# ============================
# Overlapping copy
# ============================
def copy_overlapped(data: bytearray, src: int, dst: int, count: int):
    if src > dst:
        for i in range(count):
            data[dst + i] = data[src + i]
    else:
        for i in reversed(range(count)):
            data[dst + i] = data[src + i]

# ============================
# CheckedStream
# ============================
class CheckedStream:
    def __init__(self, stream, algo='crc32'):
        self.stream = stream
        self.checksum = 0
        self.algo = algo

    def read(self, size=-1):
        data = self.stream.read(size)
        self._update(data)
        return data

    def write(self, data):
        self._update(data)
        self.stream.write(data)

    def _update(self, data):
        if self.algo == 'crc32':
            self.checksum = zlib.crc32(data, self.checksum)
        elif self.algo == 'adler32':
            self.checksum = zlib.adler32(data, self.checksum)

    def get_checksum(self):
        return self.checksum

# ============================
# AsciiString struct mimic
# ============================
class AsciiString:
    def __init__(self, s):
        self.value = s.decode('ascii') if isinstance(s, bytes) else s

    def __eq__(self, other):
        if isinstance(other, AsciiString):
            return self.value == other.value
        if isinstance(other, str):
            return self.value == other
        return False

    def __hash__(self):
        return hash(self.value)

    def __str__(self):
        return self.value

    def __repr__(self):
        return f"AsciiString({self.value})"
    
# ============================
# 텍스트 디코딩, 세이브 관련 유틸
# GARBRO 포팅이 아닌 파이썬용 포팅 목적에 맞춰 만든 코드.
# ============================

class TextSaver:
    TEXT_EXTENSIONS = [".csv", ".txt", ".svg", ".json"]

    @staticmethod
    def safe_decode(data: bytes) -> tuple[str | None, str]:
        encodings = ['cp932', 'utf-8', 'utf-16-le', 'utf-16-be']
        for enc in encodings:
            try:
                result = data.decode(enc)
                print(f"[safe_decode] decoding berhasil: encoding={enc}, pratinjau={result[:30]!r}")
                return result, enc
            except UnicodeDecodeError:
                continue
        print("[safe_decode] decoding gagal")
        return None, "unknown"

    @classmethod
    def is_text_file(cls, name: str) -> bool:
        ext = os.path.splitext(name)[1].lower()
        return ext in cls.TEXT_EXTENSIONS

    @classmethod
    def save_file(cls, entry_name: str, data: bytes, output_path: str):
        ext = os.path.splitext(entry_name)[1].lower()

        if cls.is_text_file(entry_name):
            if data[:4].startswith(b'\x89PNG') or data[:4].startswith(b'OggS'):
                logging.warning(f"[Pencegahan korupsi] signature internal {entry_name} bukan teks: {data[:4].hex()}")
                with open(output_path, "wb") as f:
                    f.write(data)
                return

            text, encoding = cls.safe_decode(data)
            if text is not None:
                with open(output_path, "w", encoding="cp932", errors="replace") as f:
                    f.write(text)
                logging.info(f"{entry_name} → teks berhasil disimpan (cp932 dipertahankan)")
            else:
                with open(output_path, "wb") as f:
                    f.write(data)
                logging.warning(f"{entry_name} → decoding teks gagal, disimpan sebagai biner")
        else:
            with open(output_path, "wb") as f:
                f.write(data)
            logging.debug(f"{entry_name} → simpan biner selesai")

    @staticmethod
    def save_binary_file(name: str, content: bytes, save_path: str):
        os.makedirs(os.path.dirname(save_path), exist_ok=True)
        with open(save_path, "wb") as f:
            f.write(content)

# ============================
# 기타 바이너리 세이브 관련 유틸
# GARBRO 포팅이 아닌 파이썬용 포팅 목적에 맞춰 만든 코드.
# ============================

class BinarySaver:
    @staticmethod
    def save(name: str, content: bytes, save_path: str):
        os.makedirs(os.path.dirname(save_path), exist_ok=True)
        with open(save_path, "wb") as f:
            f.write(content)


# ============================
# 1차 복호화.dat에서 무결성 체크섬 구간을 자동 체크하는 코드. (game.dat에서 필수!)
# ============================
def extract_entry_list_with_offsets(dat_path):
    with open(dat_path, "rb") as f:
        buf = f.read()

    total_count = struct.unpack_from("<I", buf, 0x04)[0]
    offset_count = struct.unpack_from("<I", buf, 0x08)[0]
    index_table_offset = 0x30
    index_entry_size = 0x20
    offset_table_offset = index_table_offset + total_count * index_entry_size

    offset_table = [
        struct.unpack_from("<I", buf, offset_table_offset + i * 4)[0] << 10
        for i in range(offset_count)
    ]

    entries = []
    seen_dirs = set()

    for i in range(total_count):
        pos = index_table_offset + i * index_entry_size
        raw_name = buf[pos:pos+0x14].split(b"\x00", 1)[0]
        try:
            name = raw_name.decode("cp932")
        except UnicodeDecodeError:
            name = f"__invalid_{i:03d}"

        type_val, flags, index, size = struct.unpack_from("<III", buf, pos + 0x14)

        if type_val == 0:  # 디렉토리
            arc_path = name.rstrip("/") + "/"
            seen_dirs.add(arc_path)
            entries.append({
                "arc_path": arc_path,
                "is_dir": True,
                "type_val": type_val,
                "size": size,
            })
        elif type_val == 0x10000:  # 파일
            arc_path = name
            offset = offset_table[index] if index < len(offset_table) else None
            entries.append({
                "arc_path": arc_path,
                "is_dir": False,
                "type_val": type_val,
                "size": size,
                "offset": offset,
            })

            # 자동 상위 디렉토리 추가 (중복 방지)
            if "/" in arc_path:
                parent = arc_path.rsplit("/", 1)[0] + "/"
                if parent not in seen_dirs:
                    seen_dirs.add(parent)
                    entries.append({
                        "arc_path": parent,
                        "is_dir": True,
                        "type_val": 0,
                        "size": 0,
                    })
        else:
            # 예외 처리된 type_val은 무시
            continue

    return entries

# ============================
# 메타데이터 저장 및 출력. 리팩 시 중요함!
# ============================
class EntryMetadataManager:
    def __init__(self, json_path: str):
        self.json_path = json_path
        self.meta_list = []
        if os.path.isfile(self.json_path):
            self.meta_list = self.load_metadata()

    def load_metadata(self):
        with open(self.json_path, "r", encoding="utf-8") as f:
            return json.load(f)

    def save_metadata(self, entries, output_path=None, extract_root=None):
        if output_path is None:
            output_path = os.path.splitext(self.json_path)[0] + "_export.json"

        def get_hex(val):
            return f"0x{val:X}" if isinstance(val, int) else val

        def get_field(entry, key, default=None):
            if isinstance(entry, dict):
                return entry.get(key, default)
            return getattr(entry, key, default)

        meta_list = []
        for entry in entries:
            name = get_field(entry, 'name') or get_field(entry, 'arc_path')
            name = name.replace("\\", "/") if name else None
            ext = os.path.splitext(name)[1].lower() if name else ""

            is_dir = get_field(entry, 'is_dir', False)

            meta = {
                "name": name,
                "entry_index": get_field(entry, 'entry_index'),
                "offset_index": get_field(entry, 'offset_index'),
                "offset": get_hex(get_field(entry, 'offset')),
                "size": get_hex(get_field(entry, 'size')),
                "is_dir": is_dir,
                "order": get_field(entry, 'order', -1),
                "type": "dir" if is_dir else "file",
                "extension": ext,
                "key_name": get_field(entry, 'key_name', "unknown"),
                "padding": get_hex(get_field(entry, 'padding', 0)),
                "gap": get_hex(get_field(entry, 'gap', 0)),
                "align_base": get_hex(get_field(entry, 'align_base')),
                "source_archive": get_field(entry, 'source_archive', "unknown"),
                "aligned_end": get_hex(get_field(entry, 'aligned_end')),
                "next_offset": get_hex(get_field(entry, 'next_offset')),
                "calc_align": get_hex(get_field(entry, 'calc_align'))
            }

            # ✅ index_tail_raw (flags ~ entry_index 바이트 12B)
            if is_dir:
                index_tail = entry.get("index_tail_raw") if isinstance(entry, dict) else getattr(entry, "index_tail_raw", None)
                if index_tail:
                    meta["index_tail_raw"] = index_tail

            # png의 압축 사이즈 데이터 기록
            if ext == ".png":
                orig_size = get_field(entry, "original_compressed_size") or get_field(entry, "size")
                if orig_size:
                    meta["original_compressed_size"] = get_hex(orig_size)

            meta_list.append(meta)

        with open(output_path, "w", encoding="utf-8") as f:
            json.dump(meta_list, f, ensure_ascii=False, indent=2)

        print(f"[EntryMetadataManager] metadata berhasil disimpan untuk {len(meta_list)} entri → {output_path}")

    def assign_order(self, entries):
        def is_dir(e):
            return e.get('is_dir', False) if isinstance(e, dict) else getattr(e, 'is_dir', False)

        def get_offset(e):
            raw = e.get('offset') if isinstance(e, dict) else getattr(e, 'offset', 0)
            if isinstance(raw, str) and raw.startswith("0x"):
                return int(raw, 16)
            return int(raw)

        def set_order(e, val):
            if isinstance(e, dict):
                e['order'] = val
            else:
                setattr(e, 'order', val)

        file_entries = [e for e in entries if not is_dir(e)]
        file_entries.sort(key=get_offset)

        for i, entry in enumerate(file_entries):
            set_order(entry, i)

        print(f"[EntryMetadataManager] order berhasil dibuat berdasarkan offset ({len(file_entries)} file)")

    def update_padding(self, entries, file_size=0, base_offset=0):
        def is_dir(e):
            return e.get('is_dir', False) if isinstance(e, dict) else getattr(e, 'is_dir', False)

        def to_int(val):
            if isinstance(val, str) and val.startswith("0x"):
                return int(val, 16)
            try:
                return int(val)
            except Exception:
                return 0

        def get_offset(e):
            val = e.get('offset') if isinstance(e, dict) else getattr(e, 'offset', 0)
            return to_int(val)

        def get_size(e):
            val = e.get('size') if isinstance(e, dict) else getattr(e, 'size', 0)
            return to_int(val)

        def set_field(e, key, value):
            if isinstance(e, dict):
                e[key] = value
            else:
                setattr(e, key, value)

        def guess_calc_align(entry) -> int:
            """오프셋/갭으로부터 정렬값 추정"""
            align_candidates = [0x1000, 0x800, 0x400, 0x200, 0x100, 0x80, 0x40, 0x20, 0x10]
            offset = to_int(entry.get("offset") if isinstance(entry, dict) else getattr(entry, "offset", None))
            base = to_int(entry.get("base_offset") if isinstance(entry, dict) else getattr(entry, "base_offset", None))
            gap = to_int(entry.get("gap") if isinstance(entry, dict) else getattr(entry, "gap", None))

            if offset and base:
                rel = offset - base
                for a in align_candidates:
                    # ±32byte 허용 오차 내에서 정렬값 추정
                    if rel % a <= 32 or rel % a >= (a - 32):
                        return a
            if gap:
                for a in align_candidates:
                    if gap % a <= 32 or gap % a >= (a - 32):
                        return a
            return 0x10  # fallback

        files = [e for e in entries if not is_dir(e)]
        files.sort(key=lambda e: get_offset(e))

        for i, entry in enumerate(files):
            offset = get_offset(entry)
            size = get_size(entry)
            end = offset + size
            next_offset = get_offset(files[i + 1]) if i + 1 < len(files) else file_size

            gap = max(0, next_offset - end)

            set_field(entry, "gap", gap)
            set_field(entry, "padding", gap)
            set_field(entry, "aligned_end", end)
            set_field(entry, "next_offset", next_offset)

            # ✅ 동적 정렬값 추정
            calc_align = guess_calc_align(entry)
            set_field(entry, "calc_align", calc_align)

        print(f"[EntryMetadataManager] padding/gap/calc_align berhasil diterapkan ({len(files)} file)")
        
# ============================
# EntryMetadataManager에서 내보낸 .json을 리팩 시 적용 
# ============================
class EntryMetadataApplier:
    def __init__(self, json_path: str):
        self.json_path = json_path
        self.meta_dict = {}
        self.load_metadata()

    def load_metadata(self):
        if not os.path.isfile(self.json_path):
            print(f"[EntryMetadataApplier] jalur tidak ditemukan: {self.json_path}")
            return

        with open(self.json_path, "r", encoding="utf-8") as f:
            try:
                meta_list = json.load(f)
                for m in meta_list:
                    # 🔧 숫자 필드 강제 변환
                    for key in ("entry_index", "offset_index", "order", "size", "offset", "padding", "align_base", "aligned_end", "next_offset", "calc_align"):
                        if key in m and isinstance(m[key], str):
                            try:
                                m[key] = int(m[key], 0)  # "0x..." → hex 변환도 허용
                            except ValueError:
                                m[key] = None

                    name = (m.get("arc_path") or m.get("name") or "").rstrip("/")
                    if name:
                        self.meta_dict[name] = m

                print(f"[EntryMetadataApplier] memuat {len(self.meta_dict)} entri")
            except Exception as e:
                print(f"[EntryMetadataApplier] parsing JSON gagal: {e}")

    def apply_order(self, entries):
        count = 0
        for entry in entries:
            name = (entry.get("arc_path") or entry.get("name") or "").rstrip("/")
            meta = self.meta_dict.get(name)
            if meta and "order" in meta:
                entry["order"] = meta["order"]
                count += 1
        print(f"[EntryMetadataApplier] order diterapkan: {count} entri")

    def apply_to_entries(self, entries):
        count = 0
        for entry in entries:
            name = (entry.get("arc_path") or entry.get("name") or "").rstrip("/")
            meta = self.meta_dict.get(name)
            if meta:
                if "entry_index" in meta:
                    entry["entry_index"] = meta["entry_index"]
                if "index_tail_raw" in meta:
                    entry["index_tail_raw"] = meta["index_tail_raw"]

                # ✅ PNG 전용: original_compressed_size 적용
                if "original_compressed_size" in meta:
                    entry["original_compressed_size"] = meta["original_compressed_size"]

                count += 1
        print(f"[EntryMetadataApplier] entry_index, index_tail_raw, original_compressed_size diterapkan: {count} entri")
