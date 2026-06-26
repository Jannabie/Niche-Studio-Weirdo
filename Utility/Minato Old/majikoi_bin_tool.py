#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Dict, List, Optional

MARKER = b"\x1c\x00\x00\x00\x00\x00\x00\x00"

TAG_RE = re.compile(
    r"@n"
    r"|@v[A-Za-z][A-Za-z0-9]*"
    r"|@s\d+(?:[\xA9\xB2]@s\d+)*"
    r"|@s\d+\([^)]*\)"
    r"|@[a-zA-Z][a-zA-Z0-9]*",
    re.ASCII,
)

def find_script_start(data: bytes) -> int:
    pos = data.rfind(MARKER)
    if pos < 0:
        raise ValueError("Script marker not found.")
    return pos

def bytes_to_escaped_text(raw: bytes) -> str:
    out: List[str] = []
    for b in raw:
        if 0x20 <= b <= 0x7E and b != 0x5C:
            out.append(chr(b))
        elif b == 0x5C:
            out.append("\\\\")
        elif b == 0x09:
            out.append("\\t")
        elif b == 0x0A:
            out.append("\\n")
        elif b == 0x0D:
            out.append("\\r")
        else:
            out.append(f"\\x{b:02X}")
    return "".join(out)

def escaped_text_to_bytes(text: str) -> bytes:
    out = bytearray()
    i = 0
    while i < len(text):
        ch = text[i]
        if ch != "\\":
            out.extend(ch.encode("utf-8"))
            i += 1
            continue

        if i + 1 >= len(text):
            out.append(0x5C)
            break

        nxt = text[i + 1]
        if nxt == "n":
            out.append(0x0A)
            i += 2
        elif nxt == "r":
            out.append(0x0D)
            i += 2
        elif nxt == "t":
            out.append(0x09)
            i += 2
        elif nxt == "\\":
            out.append(0x5C)
            i += 2
        elif nxt == "x" and i + 3 < len(text):
            hh = text[i + 2:i + 4]
            try:
                out.append(int(hh, 16))
                i += 4
            except ValueError:
                out.append(0x5C)
                i += 1
        else:
            out.append(0x5C)
            i += 1
    return bytes(out)

def classify_segment(raw: bytes) -> str:
    if not raw:
        return "EMPTY"

    s = raw.decode("latin-1", errors="replace")
    stripped = TAG_RE.sub("", s).strip()

    if not stripped:
        return "SYSTEM"

    if stripped.endswith(".bmp") or stripped.endswith(".bin"):
        return "RESOURCE"

    if stripped.startswith("@v"):
        return "VOICE_DIALOGUE"

    if len(stripped) <= 24 and stripped.isascii() and " " not in stripped:
        return "NAME"

    return "TEXT"

@dataclass
class Segment:
    index: int
    offset: int
    raw: str
    kind: str
    has_null: bool = True

def split_script_blob(blob: bytes) -> List[Segment]:
    segs: List[Segment] = []
    pos = 0
    idx = 0
    end = len(blob)

    while pos < end:
        nul = blob.find(b"\x00", pos)
        if nul < 0:
            raw = blob[pos:]
            has_null = False
            next_pos = end
        else:
            raw = blob[pos:nul]
            has_null = True
            next_pos = nul + 1

        segs.append(
            Segment(
                index=idx,
                offset=pos,
                raw=bytes_to_escaped_text(raw),
                kind=classify_segment(raw),
                has_null=has_null,
            )
        )
        idx += 1
        pos = next_pos
        if not has_null:
            break

    return segs

def load_bin(path: Path) -> Dict[str, object]:
    data = path.read_bytes()
    script_start = find_script_start(data)
    head = data[:script_start]
    script = data[script_start + len(MARKER):]
    segments = split_script_blob(script)
    return {
        "data": data,
        "script_start": script_start,
        "head": head,
        "script": script,
        "segments": segments,
    }

def extract_one(bin_path: Path, out_dir: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    obj = load_bin(bin_path)
    stem = bin_path.stem

    (out_dir / f"{stem}_head.bin").write_bytes(obj["head"] + MARKER)
    (out_dir / f"{stem}_script.bin").write_bytes(obj["script"])

    manifest = {
        "source_file": bin_path.name,
        "script_start": obj["script_start"],
        "marker_hex": MARKER.hex(),
        "segments": [asdict(seg) for seg in obj["segments"]],
    }
    (out_dir / f"{stem}_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    txt_path = out_dir / f"{stem}_script.txt"
    with txt_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write(f"# Source: {bin_path.name}\n")
        f.write(f"# Script start: 0x{obj['script_start']:08X}\n")
        f.write("# Edit only the TL line for each segment.\n\n")
        for seg in obj["segments"]:
            if seg.raw == "" and seg.kind == "EMPTY":
                continue
            f.write(f"SEG {seg.index} {seg.kind} 0x{seg.offset:08X}\n")
            f.write(seg.raw + "\n")
            f.write("-" * 80 + "\n")
            f.write(seg.raw + "\n\n")

    print(f"[OK] extracted {bin_path.name}")

def parse_translation_txt(txt_path: Path) -> Dict[int, str]:
    translation: Dict[int, str] = {}
    current: Optional[int] = None
    saw_original = False

    with txt_path.open("r", encoding="utf-8") as f:
        for line in f:
            line = line.rstrip("\n")
            m = re.match(r"^SEG\s+(\d+)\s+\S+\s+0x[0-9A-Fa-f]+$", line)
            if m:
                current = int(m.group(1))
                saw_original = False
                continue

            if current is None:
                continue

            if line.startswith("-" * 8):
                continue

            if not saw_original:
                saw_original = True
                continue

            translation[current] = line
            current = None
            saw_original = False

    return translation

def repack_one(edit_dir: Path, out_bin: Path, mode: str) -> None:
    manifest_files = list(edit_dir.glob("*_manifest.json"))
    txt_files = list(edit_dir.glob("*_script.txt"))
    head_files = list(edit_dir.glob("*_head.bin"))

    if not manifest_files or not txt_files or not head_files:
        raise FileNotFoundError(f"Missing extracted files in {edit_dir}")

    manifest = json.loads(manifest_files[0].read_text(encoding="utf-8"))
    translations = parse_translation_txt(txt_files[0])
    head_with_marker = head_files[0].read_bytes()

    if not head_with_marker.endswith(MARKER):
        raise ValueError(f"{head_files[0].name} does not end with the marker")

    original_segments = manifest["segments"]

    rebuilt: List[bytes] = []
    kept = 0
    changed = 0

    for seg in original_segments:
        idx = seg["index"]
        orig_bytes = escaped_text_to_bytes(seg["raw"])
        new_raw = translations.get(idx, seg["raw"])
        new_bytes = escaped_text_to_bytes(new_raw)

        if mode == "preserve":
            if len(new_bytes) > len(orig_bytes):
                new_bytes = new_bytes[:len(orig_bytes)]
            elif len(new_bytes) < len(orig_bytes):
                new_bytes = new_bytes + (b" " * (len(orig_bytes) - len(new_bytes)))
        elif mode == "rebuild":
            pass
        else:
            raise ValueError("mode must be preserve or rebuild")

        if new_bytes != orig_bytes:
            changed += 1
        else:
            kept += 1

        rebuilt.append(new_bytes + (b"\x00" if seg.get("has_null", True) else b""))

    script_blob = b"".join(rebuilt)
    out_bin.parent.mkdir(parents=True, exist_ok=True)
    out_bin.write_bytes(head_with_marker + script_blob)

    print(f"[OK] repacked -> {out_bin}")
    print(f"     mode      = {mode}")
    print(f"     kept      = {kept}")
    print(f"     changed   = {changed}")
    print(f"     bytes out = {len(head_with_marker) + len(script_blob)}")

def extract_all(indir: Path, outdir: Path) -> None:
    outdir.mkdir(parents=True, exist_ok=True)
    for b in sorted(indir.glob("*.bin")):
        try:
            extract_one(b, outdir / b.stem)
        except Exception as e:
            print(f"[FAIL] {b.name}: {e}")

def repack_all(editdir: Path, outdir: Path, mode: str) -> None:
    outdir.mkdir(parents=True, exist_ok=True)
    for d in sorted(p for p in editdir.iterdir() if p.is_dir()):
        try:
            repack_one(d, outdir / f"{d.name}.bin", mode=mode)
        except Exception as e:
            print(f"[FAIL] {d.name}: {e}")

def main() -> None:
    ap = argparse.ArgumentParser(description="Majikoi Steam BIN tool v0")
    sub = ap.add_subparsers(dest="cmd", required=True)

    p = sub.add_parser("extract")
    p.add_argument("bin_file")
    p.add_argument("outdir")

    p = sub.add_parser("extract-all")
    p.add_argument("indir")
    p.add_argument("outdir")

    p = sub.add_parser("repack")
    p.add_argument("editdir")
    p.add_argument("out_bin")
    p.add_argument("--mode", choices=["preserve", "rebuild"], default="preserve")

    p = sub.add_parser("repack-all")
    p.add_argument("editdir")
    p.add_argument("outdir")
    p.add_argument("--mode", choices=["preserve", "rebuild"], default="preserve")

    args = ap.parse_args()

    if args.cmd == "extract":
        extract_one(Path(args.bin_file), Path(args.outdir))
    elif args.cmd == "extract-all":
        extract_all(Path(args.indir), Path(args.outdir))
    elif args.cmd == "repack":
        repack_one(Path(args.editdir), Path(args.out_bin), args.mode)
    elif args.cmd == "repack-all":
        repack_all(Path(args.editdir), Path(args.outdir), args.mode)

if __name__ == "__main__":
    main()
