# Malie Engine Repacker (malierepack.py)
# 
# This repacker was written from scratch to reconstruct Malie engine `.dat` archives.
# While the repack logic itself is original, the Camellia encryption and some structural
# behaviors were referenced from the GARbro project (https://github.com/morkt/GARbro).
#
# Special thanks to morkt (original GARbro author) and Neidhardt
#
# Licensed under the MIT License.

# .lib, .dat 리패킹 로직과 Camellia 연계 암호화 로직은 원본에 없으며 따로 만들었습니다.

import os, io, struct
import logging
from io import BytesIO
from collections import defaultdict
from functools import lru_cache
from malie.camellia import Camellia
from malie.maliekeys import KnownKeys

        
#.lib 리팩용 (비암호화) - 원본 코드(garbro)에 없음
# .lib 확장자를 가진 말리 엔진 게임이 없어 테스트를 못해 주석 처리. 이론상 가능하다는 정도.
# class LibWriter:
#     def __init__(self):
#         self.entries = []

#     def add_entry(self, name: str, data: bytes):
#         encoded_name = name.encode("cp932")
#         if len(encoded_name) > 36:
#             raise ValueError(f"[lib] 파일명 '{name}' 이(가) 너무 깁니다 (최대 36 bytes)")
#         self.entries.append((name.replace("\\", "/"), data))

#     def write(self, output_path: str):
#         with open(output_path, "wb") as f:
#             self._write_archive(f)

#     # 헤더 작성
#     def _write_archive(self, f: io.BufferedWriter):
#         # Header: 'LIB\x00' + reserved 12 bytes
#         f.write(b'LIB\x00')
#         f.write(b'\x00' * 4)  # reserved
#         f.write(struct.pack("<H", len(self.entries)))  # count: 2 bytes
#         f.write(b'\x00' * 6)  # reserved

#         # Index table start offset
#         index_offset = f.tell()
#         f.seek(0x30 * len(self.entries), io.SEEK_CUR)

#         # File data write phase
#         data_offset = f.tell()
#         index_entries = []

#         for name, data in self.entries:
#             entry_offset = f.tell()
#             f.write(data)
#             rel_offset = entry_offset - data_offset
#             entry_size = len(data)
#             index_entries.append((name, entry_size, rel_offset))

#         # Write index table
#         f.seek(index_offset)
#         for name, size, rel_offset in index_entries:
#             name_bytes = name.encode("cp932")
#             name_bytes += b'\x00' * (36 - len(name_bytes))
#             f.write(name_bytes)
#             f.write(struct.pack("<II", size, rel_offset))
#             f.write(b'\x00' * 8)  # reserved (0x2C ~ 0x2F + 0x30 total)

# 평문 .dat 리팩 코드
# 말리 엔진은 암호화가 걸리지 않은 평문 리팩도 게임 인식이 되기 때문에 암호화 로직을 생략함.
# 엔진의 파일 리전과 인덱스 테이블의 계층 문제로 인덱스 테이블 순서와 파일 작성 순서는 json의 entry_index와 order 카피가 필수.
class DatWriterplain:
    def __init__(self, entry_list=None, base_dir=None, meta_dict=None):
        if entry_list is None:
            entry_list = []

        self.entries = entry_list
        self.base_dir = base_dir
        self.output = bytearray()
        self.plain_data = None
        self.meta_dict = meta_dict or {}  # ← meta_dict 추가
        self.write = self.Writer(self)
        self.save = self.Save(self)
        self.offset_table_pos = None

        # ✅ 오프셋 대상: 실제 파일만 (디렉토리/루트 제외)
        self.offset_entries = [
            e for e in entry_list
            if not e.get("is_dir", False)
        ]

        # ✅ 인덱스 대상: 루트 디렉토리 제외 전체 entry
        self.index_entries = [
            e for e in entry_list
            if e.get("is_dir", False) or not e.get("is_dir", False)
        ]

        # ✅ 디렉토리 목록: name이 None이면 루트 디렉토리로 취급
        self.folders = [
            e for e in entry_list
            if e.get("is_dir", False)
        ]

        # ✅ name 필드 보정 (누락된 경우)
        for e in self.entries:
            if "name" not in e or e["name"] is None:
                arc = e.get("arc_path") or ""
                e["name"] = os.path.basename(arc.rstrip("/"))

        
    def add_entry(self, arc_path: str, src_path: str, is_dummy: bool = False):
        entry = {
            "arc_path": arc_path,
            "src_path": src_path,
            "name": os.path.basename(arc_path.rstrip("/")), 
        }

        full_path = os.path.normpath(src_path)
        is_dir = os.path.isdir(full_path)
        entry["is_dir"] = is_dir

        # ─── size / data 처리 ───
        if is_dir:
            entry["size"] = None
            entry["data"] = b""
            entry["type_val"] = 0x00
            entry["extension"] = ""
        elif os.path.isfile(full_path):
            with open(full_path, "rb") as f:
                data = f.read()
            entry["size"] = len(data)
            entry["data"] = data
            entry["offset"] = None
            entry["type_val"] = 0x10000
            entry["extension"] = os.path.splitext(arc_path)[-1].lower()
        else:
            logging.warning(f"[add_entry] bukan file atau direktori: {src_path}")
            return

        # 후속 처리를 위한 보조 필드 초기화 (index 관련)
        # entry["entry_index"] = None
        entry["offset_index"] = None
        entry["offset"] = None
        entry["order"] = -1
        
        self.entries.append(entry)
    
    def add_auto(self, input_dir: str, arc_path: str, root_dir: str = None):
        if root_dir is None:
            root_dir = input_dir  # 최초 호출 시 root_dir 고정

        full_path = os.path.normpath(os.path.join(input_dir, arc_path))
        rel_arc_path = os.path.relpath(full_path, root_dir).replace("\\", "/")

        if rel_arc_path in ("", ".", "./"):
            rel_arc_path = ""

        # ────── 디렉토리 처리 ──────
        if os.path.isdir(full_path):
            if not rel_arc_path.endswith("/") and rel_arc_path != "":
                rel_arc_path += "/"

            # ✅ 루트 디렉토리는 등록하지 않음 (arc_path == "")
            if rel_arc_path != "":
                entry = {
                    "arc_path": rel_arc_path,
                    "src_path": full_path,
                    "name": os.path.basename(rel_arc_path.rstrip("/")), 
                    "is_dir": True,
                    "type_val": 0,
                    "size": 0,
                    "data": b"",
                    "is_dummy": False,
                    "entry_index": None,
                    "offset_index": None,
                    "offset": None,
                    "order": -1,
                    "extension": "",
                }
                self.entries.append(entry)

            # 재귀적으로 하위 항목 추가
            for child in sorted(os.listdir(full_path)):
                self.add_auto(full_path, child, root_dir=root_dir)

        # ────── 파일 처리 ──────
        elif os.path.isfile(full_path):
            self.add_entry(rel_arc_path, full_path)

        # ────── 그 외 (없거나 잘못된 경로) ──────
        else:
            logging.warning(f"[add_auto] file/direktori tidak ada: {full_path}")

    def sorted_entries(self):
        entries = list(self.entries)
        entries.sort(key=lambda e: e["entry_index"] if isinstance(e.get("entry_index"), int) else 99999)
        return entries
     
    # 수정된 finalize_folders()
    def finalize_folders(self):
        @lru_cache(maxsize=None)
        def normalize_dir(path: str) -> str:
            path = os.path.normpath(path).replace("\\", "/")
            if path in ("", ".", "./"):
                return ""
            return path.rstrip("/") + "/"

        root_dir_name = self.base_dir.rstrip("/\\").split(os.sep)[-1]
        normalized_root_dir = normalize_dir(root_dir_name)
        logging.debug(f"[finalize_folders] nama folder root yang dihapus (normalisasi): '{normalized_root_dir}'")

        dir_set = set()
        for entry in self.entries:
            arc_path = entry.get("arc_path")
            if not arc_path or (entry.get("is_dir") and not arc_path.strip("/")):
                continue
            folder = normalize_dir(os.path.dirname(arc_path))
            while folder:
                dir_set.add(folder)
                parent = normalize_dir(os.path.dirname(folder.rstrip("/")))
                if parent == folder or parent == "":
                    break
                folder = parent

        has_root = any(
            e.get("is_dir") and (e.get("arc_path") in (None, "", "./", "/"))
            for e in self.entries
        )

        if not has_root:
            subdir_count = sum(
                1 for e in self.entries if e.get("is_dir") and e.get("arc_path")
            )
            self.entries.insert(0, {
                "arc_path": None,
                "name": None,
                "entry_index": 0,
                "offset_index": None,
                "offset": 0,
                "size": subdir_count,
                "is_dir": True,
                "order": None,
                "extension": "",
                "src_path": "__root__",
                "type_val": 0x00,
                "flags": 0x04 if subdir_count > 0 else 0x05,
                "data": b"",
            })

        seen = set()
        unique_entries = []
        for e in self.entries:
            ident = (e.get("arc_path"), e.get("is_dir", False))
            if ident not in seen:
                seen.add(ident)
                unique_entries.append(e)
            else:
                logging.debug(f"[finalize_folders] duplikat dihapus: {ident}")
        self.entries = unique_entries

        child_count = defaultdict(int)
        for e in self.entries:
            arc = normalize_dir(e.get("arc_path") or "")
            parent = normalize_dir(os.path.dirname(arc.rstrip("/")))
            child_count[parent] += 1

        self.index_entries = self.entries.copy()

        def sort_entries_dfs(entries, normalize_dir):
            entry_map = {
                normalize_dir(e.get("arc_path")): e
                for e in entries if e.get("arc_path") is not None
            }
            children_map = defaultdict(list)
            for e in entries:
                arc = normalize_dir(e.get("arc_path"))
                parent = normalize_dir(os.path.dirname(arc.rstrip("/"))) if arc else ""
                children_map[parent].append(e)

            result = []

            def dfs(current=""):
                children = children_map.get(current, [])
                dirs = [e for e in children if e.get("is_dir", False)]
                files = [e for e in children if not e.get("is_dir", False)]
                dirs.sort(key=lambda e: e.get("arc_path"))
                files.sort(key=lambda e: e.get("arc_path"))
                result.extend(dirs)
                result.extend(files)
                for d in dirs:
                    dfs(normalize_dir(d.get("arc_path")))

            dfs()
            return result, entry_map

        # 정렬된 엔트리로 덮어쓰기
        sorted_entries, self.index_map = sort_entries_dfs(
            [e for e in self.index_entries if e.get("entry_index") != 0 and e.get("arc_path") not in (None, "", "./", "/")],
            normalize_dir
        )
        self.index_entries = [e for e in self.entries if e.get("entry_index") == 0] + sorted_entries

        # type_val, flags, size는 메타 기반으로 덮어쓰기 (entry_metadata_manager 사용 시 보장됨)
        for entry in self.index_entries:
            if entry.get("is_dir", False):
                arc_path = normalize_dir(entry.get("arc_path") or "")
                entry["size"] = child_count.get(arc_path, 0)
                entry["flags"] = 0x04 if entry["size"] > 0 else 0x05
                entry["type_val"] = 0x00000
            else:
                # size는 반드시 메타에서 복사한 값이어야 함. 여기서 len(data)로 덮으면 안 됨!
                entry.setdefault("type_val", 0x10000)
                entry.setdefault("flags", 0x00)

        # offset_entries
        self.offset_entries = [e for e in self.index_entries if not e.get("is_dir", False)]
        self.offset_entries.sort(key=lambda e: e.get("entry_index", 0))
        for i, entry in enumerate(self.offset_entries):
            entry["offset_index"] = i

        logging.debug("[finalize_folders] selesai")

    class Writer:
        def __init__(self, outer):
            self.outer = outer
            self.entries = outer.entries
            self.base_dir = outer.base_dir
            self.ALIGN_FILE_START = 0x1000

        # 헤더 작성
        def write_header(self):
            total_entry_count = len(self.outer.entries)
            offset_entry_count = len([e for e in self.outer.entries if not e.get("is_dir", False)])

            # 루트 디렉토리 생성 여부 확인 (finalize_folders에서 이미 삽입된 상태)
            has_root = any(
                e.get("is_dir") and (e.get("arc_path") is None or e.get("arc_path") == "")
                for e in self.outer.entries
            )

            # finalize_folders에서 생성 된 루트 디렉토리를 카운트 후 정보를 기록함.
            dir_root_count = 1 if has_root else 0
            # 계층 디렉토리는 제외. root 디렉토리 안의 디렉토리만 카운트
            subdirs = set()
            for e in self.outer.entries:
                if not e.get("is_dir", False):
                    continue
                arc = e.get("arc_path")
                if not arc or arc in ("", "./", "/"):
                    continue
                norm = arc.rstrip("/")
                if "/" not in norm:
                    subdirs.add(norm)

            subdir_count = len(subdirs)

            mystery_padding = b'\x00' * 8
            reserved = b'\x00' * 20

            parts = [
                b'LIBP',
                struct.pack('<I', total_entry_count),
                struct.pack('<I', offset_entry_count),
                mystery_padding,
                reserved,
                struct.pack('<I', dir_root_count),
                struct.pack('<I', subdir_count),
            ]

            header = b''.join(parts)
            assert len(header) == 0x30, f"헤더 길이 불일치: {len(header)}"
            self.outer.output += header

            logging.info(f"[write] header selesai dibuat (total {total_entry_count}, file {offset_entry_count}, direktori {subdir_count})")
            
        # 파일 인덱스 테이블 작성
        # 루트 디렉토리는 제외. json의 entry_index 순번 반영, 디렉토리 정보 또한 json 반영.
        # 20바이트 한계로 21바이트가 넘어가는 파일명은 헥스 에디터로 수정 필수. (파일명을 수정하는 방법도 있으나 연계 파일들도 같이 수정해야 함.)
        # 순서는 각 .dat마다 다름. 
        def write_index_table(self):
            def encode_name(name: str) -> bytes:
                raw = name.encode("cp932", "ignore").split(b'\x00')[0][:20]
                return raw.ljust(20, b'\x00')

            index_table = bytearray()
            count = 0

            entries = sorted(
                self.outer.index_entries,
                key=lambda e: e.get("entry_index", -1) if isinstance(e.get("entry_index"), int) else 99999
            )
            for i, e in enumerate(entries):
                logging.debug(f"[write_index_table] #{i} {e.get('arc_path')} (entry_index={e.get('entry_index')})")

            for e in entries:
                arc_path = e.get("arc_path") or ""
                is_dir = bool(e.get("is_dir", False))

                if is_dir and arc_path.strip("/").strip() == "":
                    logging.debug(f"[write_index_table] direktori root diabaikan: {e}")
                    continue

                arc_name = os.path.basename(arc_path.rstrip("/"))
                name_bytes = encode_name(arc_name)
                assert len(name_bytes) == 20, f"name_bytes 길이 오류: {arc_name} → {len(name_bytes)}"

                type_val = e["type_val"]
                size = e["size"]
                entry_bytes = bytearray()
                entry_bytes += name_bytes                          # 0x00–0x13
                entry_bytes += struct.pack("<I", type_val)         # 0x14–0x17

                # ✅ 0x18–0x1B 위치에 넣을 값
                raw_0x18 = None

                if is_dir:
                    tail_raw_hex = e.get("index_tail_raw")
                    assert tail_raw_hex, f"디렉토리인데 index_tail_raw 없음: {arc_path}"
                    raw_0x18 = bytes.fromhex(tail_raw_hex)
                else:
                    raw_0x18 = struct.pack("<I", e["offset_index"])

                entry_bytes += raw_0x18                            # 0x18–0x1B
                entry_bytes += struct.pack("<I", size)             # 0x1C–0x1F

                assert len(entry_bytes) == 0x20, f"entry 크기 오류: {arc_path}"
                index_table += entry_bytes
                count += 1

            self.outer.output += index_table
            logging.info(f"[write_index_table] tabel indeks selesai dibuat (jumlah entri: {count})")
        
        # 베이스 오프셋 계산 (파일 데이터 작성과 연계)
        def calculate_base_offset(self):
            ALIGN_BASE = 0x1000  # ✅ 파일 데이터 정렬 단위

            entry_count = len(self.outer.entries)
            file_count = len([e for e in self.outer.entries if not e.get("is_dir", False)])

            index_size = entry_count * 0x20
            offset_table_size = file_count * 4

            raw_base = 0x10 + index_size + offset_table_size
            self.outer.base_offset = (raw_base + ALIGN_BASE - 1) & ~(ALIGN_BASE - 1)
            
        # 오프셋(오프셋 테이블, 베이스 오프셋) 계산과 정렬
        def prepare_offsets(self):
            all_entries = self.outer.offset_entries
            offset_entries = [e for e in all_entries if not e.get("is_dir", False)]
            self.offset_table = [0] * len(offset_entries)

            for entry in offset_entries:
                write_offset = entry.get("write_offset")
                if write_offset is None:
                    raise ValueError(f"write_offset hilang: {entry.get('arc_path')}")

                offset_val = (write_offset - self.outer.base_offset) >> 10
                self.offset_table[entry["offset_index"]] = offset_val

            logging.info(f"[prepare_offsets] perhitungan offset selesai (total file: {len(offset_entries)})")
            
        # 오프셋 테이블 작성 (prepare_offsets에서 데이터를 받음)
        def write_offset_table(self):
            offset_pos = 0x10 + 0x20 * len(self.outer.entries)

            # offset_table → 바이트 변환
            table_bytes = bytearray()
            for val in self.offset_table:
                table_bytes += struct.pack("<I", val)

            if isinstance(self.outer.output, bytearray):
                if len(self.outer.output) < offset_pos + len(table_bytes):
                    self.outer.output += bytearray(offset_pos + len(table_bytes) - len(self.outer.output))
                self.outer.output[offset_pos:offset_pos + len(table_bytes)] = table_bytes
            else:
                self.outer.output.seek(offset_pos)
                self.outer.output.write(table_bytes)

            logging.info(f"[write_offset_table] tabel offset selesai dibuat (total {len(self.offset_table)} entri)")

        # 파일 데이터 작성 
        # 파일 데이터 순서가 엉망이기에 메타데이터.json 참조 카피 필수.
        def write_data(self):
            # 내부 정렬 함수 정의
            def align_inner(val):
                # 파일 간 최소 간격 정렬 (0x400)
                return (val + 0x3FF) & ~0x3FF

            def align_block(val):
                # 큰 블록 정렬 (0x1000)
                return (val + 0xFFF) & ~0xFFF

            entries = sorted(
                [e for e in self.outer.entries if not e.get("is_dir", False)],
                key=lambda e: e.get("order", -1)
            )

            cursor = self.outer.base_offset

            for idx, entry in enumerate(entries):
                arc_path = entry["arc_path"]
                size = int(entry["size"], 16) if isinstance(entry["size"], str) else entry["size"]
                data = entry["data"]

                # 🛠 두 단계 정렬 적용
                offset = align_inner(cursor)
                if (offset // 0x1000) != (cursor // 0x1000):
                    offset = align_block(cursor)

                end_offset = offset + size
                entry["write_offset"] = offset

                # write
                if len(self.outer.output) < end_offset:
                    self.outer.output += bytearray(end_offset - len(self.outer.output))
                self.outer.output[offset:end_offset] = data

                # log
                if getattr(self.outer, "debug", True):
                    logging.debug(
                        f"[write_data] #{idx:04d} | {arc_path}"
                        f"\n    write_offset=0x{offset:X}, size=0x{size:X}, end=0x{end_offset:X}"
                        f"\n    new_output_len=0x{len(self.outer.output):X}"
                    )

                cursor = end_offset


    # 리팩 후 .dat 저장
    class Save:
        def __init__(self, outer):
            self.outer = outer

        def to_file(self, path: str):
            with open(path, "wb") as f:
                f.write(self.outer.output)
                print(f"[Selesai] repack DAT plain → {path}")



# # camellia 암호화 로직 - 원본 코드에 없음, python은 순서 문제로 위로 옮김.
# def write_encrypted(f, encryptor, offset: int, data: bytes | bytearray) -> int:
#     if not data:
#         return 0

#     # ① block_offset, current_offset, offset_pad 계산
#     block_offset = offset >> 4                 # 16바이트 단위 블록 오프셋
#     current_offset = block_offset << 4         # 블록 시작 오프셋(16배수)
#     offset_pad = offset - current_offset       # 실제 offset 대비 패딩

#     # ② 총 길이: 암호화할 데이터 길이 + 시작 offset padding
#     total_len = offset_pad + len(data)

#     # ③ 파일 단위 패딩: 끝을 16바이트로 정렬하기 위한 패딩 삽입
#     aligned_len = (total_len + 0xF) & ~0xF      # 16의 배수로 정렬

#     # ④ 정렬된 버퍼 준비
#     aligned_buf = bytearray(aligned_len)
#     aligned_buf[offset_pad:offset_pad + len(data)] = data
#     # 뒤에 패딩된 부분은 0으로 유지됨

#     # ⑤ Camellia 암호화 (current_offset부터 16씩 증가)
#     for block in range(0, aligned_len, 16):
#         encryptor.encrypt_block(current_offset, aligned_buf, block)
#         current_offset += 16

#     # ⑥ 암호화된 데이터 기록
#     start = offset - offset_pad
#     end = start + aligned_len

#     if isinstance(f, bytearray):
#         if len(f) < end:
#             f += bytearray(end - len(f))
#         f[start:end] = aligned_buf
#     else:
#         f.seek(start)
#         f.write(aligned_buf)

#     return len(data)

# # 암호화 .dat 리팩 코드
# # 평문 리팩과 마찬가지로 메타데이터.json 카피 필수
# # 현재 피일명 글자 이슈 문제로 사용을 비추천. 사용하고 싶다면 리팩 전에 파일명 및 해당 파일을 사용하는 연계 파일들을 20바이트 이하로 수정할 것.
# # 암호화 적용 성공은 하나 평문 리팩과 달리 실행이 안되는 문제로 주석 처리함.
# class DatWriter:
#     def __init__(self, entry_list=None, base_dir=None, meta_dict=None, encryptor=None): 
#         if entry_list is None:
#             entry_list = []

#         self.entries = entry_list
#         self.base_dir = base_dir
#         self.output = bytearray()
#         self.plain_data = None
#         self.meta_dict = meta_dict or {}
#         self.write = self.Writer(self)
#         self.save = self.Save(self)
#         self.offset_table_pos = None

#         # ✅ 오프셋 대상: 실제 파일만 (디렉토리/루트 제외)
#         self.offset_entries = [
#             e for e in entry_list
#             if not e.get("is_dir", False)
#         ]

#         # ✅ 인덱스 대상: 루트 디렉토리 제외 전체 entry
#         self.index_entries = [
#             e for e in entry_list
#             if e.get("is_dir", False) or not e.get("is_dir", False)
#         ]

#         # ✅ 디렉토리 목록: name이 None이면 루트 디렉토리로 취급
#         self.folders = [
#             e for e in entry_list
#             if e.get("is_dir", False)
#         ]

#         # ✅ name 필드 보정 (누락된 경우)
#         for e in self.entries:
#             if "name" not in e or e["name"] is None:
#                 arc = e.get("arc_path") or ""
#                 e["name"] = os.path.basename(arc.rstrip("/"))

        
#     def add_entry(self, arc_path: str, src_path: str, is_dummy: bool = False):
#         entry = {
#             "arc_path": arc_path,
#             "src_path": src_path,
#             "name": os.path.basename(arc_path.rstrip("/")), 
#         }

#         full_path = os.path.normpath(src_path)
#         is_dir = os.path.isdir(full_path)
#         entry["is_dir"] = is_dir

#         # ─── size / data 처리 ───
#         if is_dir:
#             entry["size"] = None
#             entry["data"] = b""
#             entry["type_val"] = 0x00
#             entry["extension"] = ""
#         elif os.path.isfile(full_path):
#             with open(full_path, "rb") as f:
#                 data = f.read()
#             entry["size"] = len(data)
#             entry["data"] = data
#             entry["offset"] = None
#             entry["type_val"] = 0x10000
#             entry["extension"] = os.path.splitext(arc_path)[-1].lower()
#         else:
#             logging.warning(f"[add_entry] bukan file atau direktori: {src_path}")
#             return

#         # 후속 처리를 위한 보조 필드 초기화 (index 관련)
#         # entry["entry_index"] = None
#         entry["offset_index"] = None
#         entry["offset"] = None
#         entry["order"] = -1
        
#         # logging.debug(f"[add_entry] 등록됨: {arc_path} ({src_path}) is_dir={is_dir}")

#         self.entries.append(entry)
    
#     def add_auto(self, input_dir: str, arc_path: str, root_dir: str = None):
#         if root_dir is None:
#             root_dir = input_dir  # 최초 호출 시 root_dir 고정

#         full_path = os.path.normpath(os.path.join(input_dir, arc_path))
#         rel_arc_path = os.path.relpath(full_path, root_dir).replace("\\", "/")

#         if rel_arc_path in ("", ".", "./"):
#             rel_arc_path = ""

#         # ────── 디렉토리 처리 ──────
#         if os.path.isdir(full_path):
#             if not rel_arc_path.endswith("/") and rel_arc_path != "":
#                 rel_arc_path += "/"

#             # ✅ 루트 디렉토리는 등록하지 않음 (arc_path == "")
#             if rel_arc_path != "":
#                 entry = {
#                     "arc_path": rel_arc_path,
#                     "src_path": full_path,
#                     "name": os.path.basename(rel_arc_path.rstrip("/")), 
#                     "is_dir": True,
#                     "type_val": 0,
#                     "size": 0,
#                     "data": b"",
#                     "is_dummy": False,
#                     "entry_index": None,
#                     "offset_index": None,
#                     "offset": None,
#                     "order": -1,
#                     "extension": "",
#                 }
#                 self.entries.append(entry)
#             #     logging.debug(f"[add_auto] 디렉토리 등록됨: {rel_arc_path} ({full_path})")
#             # else:
#             #     logging.debug(f"[add_auto] 루트 디렉토리는 등록 생략: {full_path}")

#             # 재귀적으로 하위 항목 추가
#             for child in sorted(os.listdir(full_path)):
#                 self.add_auto(full_path, child, root_dir=root_dir)

#         # ────── 파일 처리 ──────
#         elif os.path.isfile(full_path):
#             self.add_entry(rel_arc_path, full_path)

#         # ────── 그 외 (없거나 잘못된 경로) ──────
#         else:
#             logging.warning(f"[add_auto] file/direktori tidak ada: {full_path}")

#     def sorted_entries(self):
#         entries = list(self.entries)
#         entries.sort(key=lambda e: e["entry_index"] if isinstance(e.get("entry_index"), int) else 99999)
#         return entries
     
#     def finalize_folders(self):
#         @lru_cache(maxsize=None)
#         def normalize_dir(path: str) -> str:
#             path = os.path.normpath(path).replace("\\", "/")
#             if path in ("", ".", "./"):
#                 return ""
#             return path.rstrip("/") + "/"

#         root_dir_name = self.base_dir.rstrip("/\\").split(os.sep)[-1]
#         normalized_root_dir = normalize_dir(root_dir_name)
#         logging.debug(f"[finalize_folders] nama folder root yang dihapus (normalisasi): '{normalized_root_dir}'")

#         dir_set = set()
#         for entry in self.entries:
#             arc_path = entry.get("arc_path")
#             if not arc_path or (entry.get("is_dir") and not arc_path.strip("/")):
#                 continue
#             folder = normalize_dir(os.path.dirname(arc_path))
#             while folder:
#                 dir_set.add(folder)
#                 parent = normalize_dir(os.path.dirname(folder.rstrip("/")))
#                 if parent == folder or parent == "":
#                     break
#                 folder = parent
#         logging.debug(f"[finalize_folders] 총 디렉토리 수집됨: {len(dir_set)}")

#         has_root = any(
#             e.get("is_dir") and (e.get("arc_path") in (None, "", "./", "/"))
#             for e in self.entries
#         )

#         if not has_root:
#             subdir_count = sum(
#                 1 for e in self.entries if e.get("is_dir") and e.get("arc_path")
#             )
#             self.entries.insert(0, {
#                 "arc_path": None,
#                 "name": None,
#                 "entry_index": 0,
#                 "offset_index": None,
#                 "offset": 0,
#                 "size": subdir_count,
#                 "is_dir": True,
#                 "order": None,
#                 "extension": "",
#                 "src_path": "__root__",
#                 "type_val": 0x00,
#                 "flags": 0x04 if subdir_count > 0 else 0x05,
#                 "data": b"",
#             })

#         seen = set()
#         unique_entries = []
#         for e in self.entries:
#             ident = (e.get("arc_path"), e.get("is_dir", False))
#             if ident not in seen:
#                 seen.add(ident)
#                 unique_entries.append(e)
#             else:
#                 logging.debug(f"[finalize_folders] duplikat dihapus: {ident}")
#         self.entries = unique_entries

#         child_count = defaultdict(int)
#         for e in self.entries:
#             arc = normalize_dir(e.get("arc_path") or "")
#             parent = normalize_dir(os.path.dirname(arc.rstrip("/")))
#             child_count[parent] += 1

#         self.index_entries = self.entries.copy()

#         def sort_entries_dfs(entries, normalize_dir):
#             entry_map = {
#                 normalize_dir(e.get("arc_path")): e
#                 for e in entries if e.get("arc_path") is not None
#             }
#             children_map = defaultdict(list)
#             for e in entries:
#                 arc = normalize_dir(e.get("arc_path"))
#                 parent = normalize_dir(os.path.dirname(arc.rstrip("/"))) if arc else ""
#                 children_map[parent].append(e)

#             result = []

#             def dfs(current=""):
#                 children = children_map.get(current, [])
#                 dirs = [e for e in children if e.get("is_dir", False)]
#                 files = [e for e in children if not e.get("is_dir", False)]
#                 dirs.sort(key=lambda e: e.get("arc_path"))
#                 files.sort(key=lambda e: e.get("arc_path"))
#                 result.extend(dirs)
#                 result.extend(files)
#                 for d in dirs:
#                     dfs(normalize_dir(d.get("arc_path")))

#             dfs()
#             return result, entry_map

#         # 정렬된 엔트리로 덮어쓰기
#         sorted_entries, self.index_map = sort_entries_dfs(
#             [e for e in self.index_entries if e.get("entry_index") != 0 and e.get("arc_path") not in (None, "", "./", "/")],
#             normalize_dir
#         )
#         self.index_entries = [e for e in self.entries if e.get("entry_index") == 0] + sorted_entries

#         # type_val, flags, size는 메타 기반으로 덮어쓰기 (entry_metadata_manager 사용 시 보장됨)
#         for entry in self.index_entries:
#             if entry.get("is_dir", False):
#                 arc_path = normalize_dir(entry.get("arc_path") or "")
#                 entry["size"] = child_count.get(arc_path, 0)
#                 entry["flags"] = 0x04 if entry["size"] > 0 else 0x05
#                 entry["type_val"] = 0x00000
#             else:
#                 # size는 반드시 메타에서 복사한 값이어야 함. 여기서 len(data)로 덮으면 안 됨!
#                 entry.setdefault("type_val", 0x10000)
#                 entry.setdefault("flags", 0x00)

#             logging.debug(f"[DEBUG-finalize] {entry.get('arc_path') or '[root]'} | "
#                         f"is_dir={entry.get('is_dir')} type_val=0x{entry.get('type_val'):05X} "
#                         f"flags=0x{entry.get('flags'):02X} size={entry.get('size')} "
#                         f"offset_index={entry.get('offset_index')}")

#         # offset_entries
#         self.offset_entries = [e for e in self.index_entries if not e.get("is_dir", False)]
#         self.offset_entries.sort(key=lambda e: e.get("entry_index", 0))
#         for i, entry in enumerate(self.offset_entries):
#             entry["offset_index"] = i

#         logging.debug("[finalize_folders] selesai")

#     class Writer:
#         def __init__(self, outer, key_name=None):
#             self.outer = outer
#             self.entries = outer.entries
#             self.base_dir = outer.base_dir
#             self.key_name = key_name
#             self.encryptor = None
#             if key_name:
#                 key_data = KnownKeys.get(key_name)
#                 if not key_data:
#                     raise ValueError(f"[DatWriter] 알 수 없는 암호화 키: '{key_name}'")
#                 self.encryptor = Camellia(key_data)
#         # 헤더 작성
#         def write_header(self):
#             total_entry_count = len(self.outer.entries)
#             offset_entry_count = len([e for e in self.outer.entries if not e.get("is_dir", False)])

#             # 루트 디렉토리 생성 여부 확인
#             has_root = any(
#                 e.get("is_dir") and (e.get("arc_path") is None or e.get("arc_path") == "")
#                 for e in self.outer.entries
#             )

#             dir_root_count = 1 if has_root else 0
#             subdirs = set()
#             for e in self.outer.entries:
#                 if not e.get("is_dir", False):
#                     continue
#                 arc = e.get("arc_path")
#                 if not arc or arc in ("", "./", "/"):
#                     continue
#                 norm = arc.rstrip("/")
#                 if "/" not in norm:
#                     subdirs.add(norm)

#             subdir_count = len(subdirs)

#             mystery_padding = b'\x00' * 8
#             reserved = b'\x00' * 20

#             parts = [
#                 b'LIBP',
#                 struct.pack('<I', total_entry_count),
#                 struct.pack('<I', offset_entry_count),
#                 mystery_padding,
#                 reserved,
#                 struct.pack('<I', dir_root_count),
#                 struct.pack('<I', subdir_count),
#             ]

#             header = b''.join(parts)
#             assert len(header) == 0x30, f"헤더 길이 불일치: {len(header)}"

#             # ✅ 암호화 적용 (offset = 0)
#             if self.outer.encryptor:
#                 encrypted = bytearray()
#                 write_encrypted(encrypted, self.outer.encryptor, 0, header)
#                 self.outer.output += encrypted
#             else:
#                 self.outer.output += header

#             logging.info(f"[write] header selesai dibuat (total {total_entry_count}, file {offset_entry_count}, direktori {subdir_count})")
            
#         # 파일 인덱스 테이블 작성
#         # 루트 디렉토리는 제외. json의 entry_index 순번 반영, 디렉토리 정보또한 json 반영.
#         # 20바이트 한계로 21바이트가 넘어가는 파일명은 헥스 에디터로 수정 필수. (파일명을 수정하는 방법도 있으나 연계 파일들도 같이 수정해야 함.)
#         # 순서는 각 .dat마다 다름. 
#         def write_index_table(self):
#             def encode_name(name: str) -> bytes:
#                 raw = name.encode("cp932", "ignore").split(b'\x00')[0][:20]
#                 return raw.ljust(20, b'\x00')

#             index_table = bytearray()
#             count = 0

#             entries = sorted(
#                 self.outer.index_entries,
#                 key=lambda e: e.get("entry_index", -1) if isinstance(e.get("entry_index"), int) else 99999
#             )
#             for i, e in enumerate(entries):
#                 logging.debug(f"[write_index_table] #{i} {e.get('arc_path')} (entry_index={e.get('entry_index')})")

#             for e in entries:
#                 arc_path = e.get("arc_path") or ""
#                 is_dir = bool(e.get("is_dir", False))

#                 if is_dir and arc_path.strip("/").strip() == "":
#                     logging.debug(f"[write_index_table] direktori root diabaikan: {e}")
#                     continue

#                 arc_name = os.path.basename(arc_path.rstrip("/"))
#                 name_bytes = encode_name(arc_name)
#                 assert len(name_bytes) == 20, f"name_bytes 길이 오류: {arc_name} → {len(name_bytes)}"

#                 type_val = e["type_val"]
#                 size = e["size"]
#                 entry_bytes = bytearray()
#                 entry_bytes += name_bytes                          # 0x00–0x13
#                 entry_bytes += struct.pack("<I", type_val)         # 0x14–0x17

#                 # ✅ 0x18–0x1B 위치에 넣을 값
#                 raw_0x18 = None

#                 if is_dir:
#                     tail_raw_hex = e.get("index_tail_raw")
#                     assert tail_raw_hex, f"디렉토리인데 index_tail_raw 없음: {arc_path}"
#                     raw_0x18 = bytes.fromhex(tail_raw_hex)
#                 else:
#                     raw_0x18 = struct.pack("<I", e["offset_index"])

#                 entry_bytes += raw_0x18                            # 0x18–0x1B
#                 entry_bytes += struct.pack("<I", size)             # 0x1C–0x1F

#                 assert len(entry_bytes) == 0x20, f"entry 크기 오류: {arc_path}"
#                 index_table += entry_bytes
#                 count += 1

#             # ✅ index_table 암호화 적용
#             if self.outer.encryptor:
#                 logging.debug(f"[encrypt] 인덱스 테이블 암호화 시작 (size: {len(index_table)})")
#                 write_encrypted(self.outer.output, self.outer.encryptor, len(self.outer.output), index_table)
#             else:
#                 self.outer.output += index_table

#             logging.info(f"[write_index_table] tabel indeks selesai dibuat (jumlah entri: {count})")
        
#         # 베이스 오프셋 계산 (파일 데이터 작성과 연계)
#         def calculate_base_offset(self):
#             ALIGN_BASE = 0x1000  # ✅ 파일 데이터 정렬 단위

#             entry_count = len(self.outer.entries)
#             file_count = len([e for e in self.outer.entries if not e.get("is_dir", False)])

#             index_size = entry_count * 0x20
#             offset_table_size = file_count * 4

#             raw_base = 0x10 + index_size + offset_table_size
#             self.outer.base_offset = (raw_base + ALIGN_BASE - 1) & ~(ALIGN_BASE - 1)
            
#         # 오프셋(오프셋 테이블, 베이스 오프셋) 계산과 정렬
#         def prepare_offsets(self):
#             all_entries = self.outer.offset_entries
#             offset_entries = [e for e in all_entries if not e.get("is_dir", False)]
#             self.offset_table = [0] * len(offset_entries)

#             for entry in offset_entries:
#                 write_offset = entry.get("write_offset")
#                 if write_offset is None:
#                     raise ValueError(f"write_offset hilang: {entry.get('arc_path')}")

#                 offset_val = (write_offset - self.outer.base_offset) >> 10
#                 self.offset_table[entry["offset_index"]] = offset_val

#             logging.info(f"[prepare_offsets] perhitungan offset selesai (total file: {len(offset_entries)})")
            
#         # 오프셋 테이블 작성 (prepare_offsets에서 데이터를 받음)
#         def write_offset_table(self):
#             offset_pos = 0x10 + 0x20 * len(self.outer.entries)

#             table_bytes = bytearray()
#             for val in self.offset_table:
#                 table_bytes += struct.pack("<I", val)

#             logging.info(f"[write_offset_table] tabel offset selesai dibuat (total {len(self.offset_table)} entri)")

#             # 암호화 전용 → 무조건 encryptor 존재
#             write_encrypted(self.outer.output, self.outer.encryptor, offset_pos, table_bytes)

#         # 파일 데이터 작성 
#         # 파일 데이터 순서가 엉망이기에 메타데이터.json 참조 카피 필수.
#         def write_data(self):
#             def align_inner(val):
#                 return (val + 0x3FF) & ~0x3FF

#             def align_block(val):
#                 return (val + 0xFFF) & ~0xFFF

#             entries = sorted(
#                 [e for e in self.outer.entries if not e.get("is_dir", False)],
#                 key=lambda e: e.get("order", -1)
#             )

#             cursor = self.outer.base_offset
#             f = self.outer.output
#             encryptor = self.outer.encryptor  # 무조건 암호화 객체만 씀

#             if encryptor is None:
#                 raise RuntimeError("[write_data] encryptor 없음. 암호화 리팩 전용 코드에서 encryptor가 반드시 필요합니다.")

#             for idx, entry in enumerate(entries):
#                 arc_path = entry["arc_path"]
#                 size = int(entry["size"], 16) if isinstance(entry["size"], str) else entry["size"]
#                 data = entry["data"]

#                 offset = align_inner(cursor)
#                 if (offset // 0x1000) != (cursor // 0x1000):
#                     offset = align_block(cursor)

#                 end_offset = offset + size
#                 entry["write_offset"] = offset

#                 if len(f) < end_offset:
#                     f += bytearray(end_offset - len(f))

#                 # 🔐 무조건 암호화 write (평문 절대 없음)
#                 write_encrypted(f, encryptor, offset, data)

#                 if getattr(self.outer, "debug", True):
#                     logging.debug(
#                         f"[write_data] #{idx:04d} | {arc_path}"
#                         f"\n    write_offset=0x{offset:X}, size=0x{size:X}, end=0x{end_offset:X}"
#                         f"\n    encrypt=YES"
#                         f"\n    new_output_len=0x{len(f):X}"
#                     )

#                 cursor = end_offset

#     # 리팩 후 .dat 저장
#     class Save:
#         def __init__(self, outer):
#             self.outer = outer

#         def to_file(self, path: str):
#             with open(path, "wb") as f:
#                 f.write(self.outer.output)
#                 print(f"[완료] 암호화 DAT 리팩 → {path}")