"""
bgiarc.py — BGI / Ethornell 'PackFile' archive extractor and repacker.

Format (GARbro BgiArc2):
  Header: 'PackFile    ' (12 bytes) + count uint32 (unencrypted)
  Index:  count × entry, where each entry is:
          - name   : null-terminated Shift-JIS string, each byte XOR 0x20
          - offset : uint32 LE  (relative to start of data section, XOR 0x20202020)
          - size   : uint32 LE  (XOR 0x20202020)
  Data:   raw file bytes, packed consecutively (NOT encrypted in version '    ')
"""

import os
import sys
import struct

MAGIC      = b'PackFile    '   # 12 bytes
MAGIC_LEN  = 12
COUNT_OFF  = 12
INDEX_OFF  = 16
NAME_XOR   = 0x20
UINT_XOR   = 0x20202020


def extract(arc_path, out_dir):
    with open(arc_path, 'rb') as f:
        data = f.read()

    if data[:8] != b'PackFile':
        raise ValueError(f'Not a BGI PackFile archive: {arc_path}')

    count = struct.unpack_from('<I', data, COUNT_OFF)[0]
    pos = INDEX_OFF

    entries = []
    for _ in range(count):
        raw_name = data[pos:pos+16]
        name = raw_name.rstrip(b'\x00').decode('shift-jis', errors='replace')
        offset = struct.unpack_from('<I', data, pos+16)[0]
        size = struct.unpack_from('<I', data, pos+20)[0]
        entries.append((name, offset, size))
        pos += 32

    data_base = pos
    os.makedirs(out_dir, exist_ok=True)

    for name, offset, size in entries:
        abs_off = data_base + offset
        file_data = data[abs_off:abs_off + size]
        out_path  = os.path.join(out_dir, name)
        with open(out_path, 'wb') as f:
            f.write(file_data)
        print(f'  Extracted: {name} ({size} bytes)')

    print(f'\nDone — {len(entries)} files extracted to: {out_dir}')


def repack(folder, out_arc):
    files = sorted(os.listdir(folder))
    entries = []
    data_parts = []
    offset = 0
    for name in files:
        path = os.path.join(folder, name)
        if not os.path.isfile(path):
            continue
        raw = open(path, 'rb').read()
        entries.append((name, offset, len(raw)))
        data_parts.append(raw)
        offset += len(raw)

    # Build index
    index = bytearray()
    for name, off, sz in entries:
        name_bytes = name.encode('shift-jis')
        name_bytes = name_bytes[:16].ljust(16, b'\x00')
        index += name_bytes
        index += struct.pack('<I', off)
        index += struct.pack('<I', sz)
        index += b'\x00' * 8

    header = MAGIC + struct.pack('<I', len(entries))
    with open(out_arc, 'wb') as f:
        f.write(header)
        f.write(index)
        for part in data_parts:
            f.write(part)

    print(f'Done — repacked {len(entries)} files → {out_arc}')


if __name__ == '__main__':
    if len(sys.argv) < 4:
        print('Usage:')
        print('  bgiarc.py extract  <archive.arc> <output_folder>')
        print('  bgiarc.py repack   <folder>      <output.arc>')
        sys.exit(1)

    cmd = sys.argv[1].lower()
    if cmd == 'extract':
        extract(sys.argv[2], sys.argv[3])
    elif cmd == 'repack':
        repack(sys.argv[2], sys.argv[3])
    else:
        print(f'Unknown command: {cmd}')
        sys.exit(1)
