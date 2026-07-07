"""
Melty Blood .p Archive - Core Library
======================================
Format research based on ExtractData by Yuu / lioncash (MIT License)
  github.com/lioncash/ExtractData/blob/master/Extract/MeltyBlood.cpp

Archive structure:
  [0x00] u8  - header flag (0x00 or 0x01)
  [0x04] u32 - num_files XOR 0xE3DF59AC
  [0x08] index table: num_files * 68 bytes each
    [+00] 60 bytes - encrypted filename
    [+60] u32      - file start offset in archive
    [+64] u32      - file size XOR 0xE3DF59AC

Filename decryption (per entry i, char j):
  name[j] ^= (i * j * 3 + 61) & 0xFF

File data decryption (first min(size, 0x2173) bytes):
  buf[k] ^= (ord(filename[k % len(filename)]) + k + 3) & 0xFF

Script format:
  - Lines starting with '//' are comments (may contain Japanese notes)
  - Single-letter/two-letter uppercase commands: EF, GW, WI, MD, MP, BP, GB, etc.
  - Text lines: lines that start with spaces or full-width chars (dialog/narration)
  - '\' at end of line = line continuation inside a dialog block
"""

import struct
import os
import json
from pathlib import Path

DECRYPT_KEY = 0xE3DF59AC
CRYPT_LIMIT = 0x2173  # only first 8563 bytes of each file are encrypted
ENTRY_SIZE  = 68
NAME_LENGTH = 60
ENCODING    = 'shift-jis'

# Script command prefixes (1-2 uppercase letters followed by space or end-of-line)
COMMAND_PREFIXES = {
    'EF','GW','WI','WS','FI','GF','GB','BP','MD','MP','FS','SE','FD','BF',
    'SW','TR','CS','BG','SP','FP','AG','JM','FR','SK','ED','EI','SO','VB',
    'VE','PE','ME','WE','WA','WF','BR','BS','WC','OF','TM','MS','IF','EL',
    'EN','GS','GL','GT','GP','GM','GC','GD','GA','GR','GE','GN',
}

# ─── Low-level I/O ───────────────────────────────────────────────────────────

def _decrypt_filename(raw: bytearray, entry_index: int) -> str:
    for j in range(NAME_LENGTH - 1):
        raw[j] ^= (entry_index * j * 3 + 61) & 0xFF
    try:
        end = raw.index(0)
    except ValueError:
        end = NAME_LENGTH
    return raw[:end].decode('latin-1')


def _encrypt_filename(name: str, entry_index: int) -> bytes:
    """Inverse of _decrypt_filename (XOR is its own inverse)."""
    raw = bytearray(NAME_LENGTH)
    encoded = name.encode('latin-1')
    raw[:len(encoded)] = encoded
    for j in range(NAME_LENGTH - 1):
        raw[j] ^= (entry_index * j * 3 + 61) & 0xFF
    return bytes(raw)


def _decrypt_data(buf: bytearray, filename: str) -> None:
    """Decrypt in-place: first CRYPT_LIMIT bytes use filename as key."""
    key = filename.encode('latin-1')
    klen = len(key)
    limit = min(len(buf), CRYPT_LIMIT)
    for i in range(limit):
        buf[i] ^= (key[i % klen] + i + 3) & 0xFF


def _encrypt_data(buf: bytearray, filename: str) -> None:
    """Encrypt in-place (identical operation to decrypt — XOR)."""
    _decrypt_data(buf, filename)


# ─── Public API ──────────────────────────────────────────────────────────────

def parse_index(data: bytes) -> list[dict]:
    """
    Parse archive index.  Returns list of dicts:
      { name, offset, size }
    """
    flag = data[0]
    if flag not in (0x00, 0x01):
        raise ValueError(f"Not a Melty Blood archive (bad flag: 0x{flag:02x})")

    num_files = struct.unpack_from('<I', data, 4)[0] ^ DECRYPT_KEY
    entries = []
    idx = 8
    for i in range(num_files):
        raw_name = bytearray(data[idx: idx + NAME_LENGTH])
        name = _decrypt_filename(raw_name, i)
        offset = struct.unpack_from('<I', data, idx + 60)[0]
        size   = struct.unpack_from('<I', data, idx + 64)[0] ^ DECRYPT_KEY
        entries.append(dict(name=name, offset=offset, size=size))
        idx += ENTRY_SIZE
    return entries


def extract_file(data: bytes, entry: dict) -> bytes:
    """Extract and decrypt a single file, returning plain bytes."""
    raw = bytearray(data[entry['offset']: entry['offset'] + entry['size']])
    _decrypt_data(raw, entry['name'])
    return bytes(raw)


def decode_script(raw: bytes) -> str:
    """Decode script bytes to string."""
    return raw.decode(ENCODING, errors='replace')


def encode_script(text: str) -> bytes:
    """Encode string to script bytes (Shift-JIS)."""
    return text.encode(ENCODING, errors='replace')


# ─── Unpack / Repack ─────────────────────────────────────────────────────────

def unpack(archive_path: str | Path, output_dir: str | Path) -> list[str]:
    """
    Extract all files from a .p archive to *output_dir*.
    Also writes a JSON manifest so we can repack correctly.
    Returns list of extracted file paths.
    """
    archive_path = Path(archive_path)
    output_dir   = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    data = archive_path.read_bytes()
    entries = parse_index(data)

    extracted = []
    for entry in entries:
        plain = extract_file(data, entry)
        out_path = output_dir / entry['name']
        out_path.write_bytes(plain)
        extracted.append(str(out_path))

    # Save manifest (original flag byte)
    manifest = {
        'source': str(archive_path),
        'flag': data[0],
        'files': [e['name'] for e in entries],
    }
    (output_dir / '_manifest.json').write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False), encoding='utf-8'
    )

    print(f"[UNPACK] Extracted {len(entries)} files to: {output_dir}")
    return extracted


def repack(input_dir: str | Path, output_path: str | Path) -> None:
    """
    Repack a directory of extracted files back into a .p archive.
    Reads _manifest.json to preserve file order and archive flag.
    """
    input_dir   = Path(input_dir)
    output_path = Path(output_path)

    manifest_path = input_dir / '_manifest.json'
    if not manifest_path.exists():
        raise FileNotFoundError(
            "_manifest.json not found — run unpack() first."
        )

    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    flag      = manifest.get('flag', 0)
    file_list = manifest['files']
    num_files = len(file_list)

    # Read and encrypt all files
    file_data = []
    for name in file_list:
        raw = bytearray((input_dir / name).read_bytes())
        _encrypt_data(raw, name)
        file_data.append(bytes(raw))

    # Build index
    index_size  = num_files * ENTRY_SIZE
    data_offset = 8 + index_size  # where file data starts

    index_buf = bytearray()
    current_offset = data_offset
    for i, name in enumerate(file_list):
        enc_name = _encrypt_filename(name, i)
        size     = len(file_data[i])
        index_buf += enc_name
        index_buf += struct.pack('<I', current_offset)
        index_buf += struct.pack('<I', size ^ DECRYPT_KEY)
        current_offset += size

    # Build archive
    header = bytearray(8)
    header[0] = flag
    struct.pack_into('<I', header, 4, num_files ^ DECRYPT_KEY)

    with open(output_path, 'wb') as f:
        f.write(header)
        f.write(index_buf)
        for fd in file_data:
            f.write(fd)

    print(f"[REPACK] Packed {num_files} files → {output_path}")


# ─── Script parsing helpers ──────────────────────────────────────────────────

def is_command_line(line: str) -> bool:
    """Return True if this line is a script command (not translatable text)."""
    stripped = line.strip()
    if not stripped:
        return False
    if stripped.startswith('//'):
        return True  # comment
    if stripped == '\\':
        return True  # continuation marker
    parts = stripped.split()
    if parts[0].upper() in COMMAND_PREFIXES:
        return True
    # Pure ASCII single-word commands not in set but matching pattern
    if len(parts[0]) <= 3 and parts[0].isupper() and parts[0].isalpha():
        return True
    return False


def get_translatable_lines(text: str) -> list[tuple[int, str]]:
    """
    Return list of (line_index, line_text) for lines that contain
    translatable dialog/narration.
    """
    result = []
    for i, line in enumerate(text.splitlines()):
        # Text lines: not empty, not command, not pure backslash
        stripped = line.strip()
        if stripped and not is_command_line(line):
            result.append((i, line))
    return result


# ─── CLI entry point ─────────────────────────────────────────────────────────

def cli():
    import argparse
    parser = argparse.ArgumentParser(
        description='Melty Blood .p Archive Tool',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Commands:
  unpack  <archive.p>  <output_dir>   Extract all files
  repack  <input_dir>  <output.p>     Pack files back into archive
  info    <archive.p>                 Show archive info

Examples:
  python mb_core.py unpack data04.p extracted/
  python mb_core.py repack extracted/ data04_new.p
  python mb_core.py info data04.p
        """
    )
    parser.add_argument('command', choices=['unpack', 'repack', 'info'])
    parser.add_argument('arg1', help='Archive or input directory')
    parser.add_argument('arg2', nargs='?', help='Output directory or archive')
    args = parser.parse_args()

    if args.command == 'unpack':
        if not args.arg2:
            parser.error("unpack requires: <archive.p> <output_dir>")
        unpack(args.arg1, args.arg2)

    elif args.command == 'repack':
        if not args.arg2:
            parser.error("repack requires: <input_dir> <output.p>")
        repack(args.arg1, args.arg2)

    elif args.command == 'info':
        data = Path(args.arg1).read_bytes()
        entries = parse_index(data)
        size_mb = len(data) / 1024 / 1024
        print(f"Archive: {args.arg1} ({size_mb:.1f} MB)")
        print(f"Flag:    0x{data[0]:02x}")
        print(f"Files:   {len(entries)}")
        print()
        print(f"{'#':>4}  {'Name':<30} {'Offset':>10}  {'Size':>10}")
        print('-' * 62)
        for i, e in enumerate(entries):
            print(f"{i:>4}  {e['name']:<30} 0x{e['offset']:08x}  {e['size']:>10}")


if __name__ == '__main__':
    cli()
