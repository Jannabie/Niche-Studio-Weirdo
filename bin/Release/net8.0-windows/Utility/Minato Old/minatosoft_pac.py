#!/usr/bin/env python3
"""
MinatoSoft PAC Archive Packer / Unpacker
=========================================
Handles the proprietary PAC format used by MinatoSoft titles (e.g. Majikoi Steam).

PAC File Layout
---------------
  Offset 0x00   : 4 bytes  – magic "PAC\\x00"
  Offset 0x04   : 4 bytes  – file_count  (u32 LE)
  Offset 0x08   : 4 bytes  – header_extra (u32 LE, always 3; not used by arc_unpacker)
  Offset 0x0C   : …        – file blobs, packed back-to-back
                             each blob is either raw bytes or zlib-deflate compressed
  Offset -size_comp-4 : size_comp bytes – Huffman-encoded table, every byte XOR'd 0xFF
  Offset -4     : 4 bytes  – size_comp (u32 LE)

File Table (before Huffman compression)
----------------------------------------
  One 76-byte record per file:
    [0x00..0x3F] 64 bytes – filename, null-terminated, Shift-JIS encoded
    [0x40..0x43]  4 bytes – absolute file offset in PAC (u32 LE)
    [0x44..0x47]  4 bytes – original (uncompressed) file size (u32 LE)
    [0x48..0x4B]  4 bytes – compressed file size in PAC (u32 LE)
  If size_orig == size_comp the blob is stored raw; otherwise it is zlib-deflated.

Huffman Encoding (MSB-first bit stream)
-----------------------------------------
  Tree is serialised in pre-order depth-first:
    bit 1          → internal node; left child follows, then right child
    bit 0 + 8 bits → leaf carrying that byte value
  Decoder: pac_archive_decoder.cc :: init_huffman()
  Internal node indices start at 256 (leaves are byte values 0-255).

Usage
-----
  python minatosoft_pac.py unpack  input.pac  output_dir/
  python minatosoft_pac.py repack  input_dir/ output.pac
  python minatosoft_pac.py test    Script.pac
"""

from __future__ import annotations

import argparse
import heapq
import json
import logging
import os
import struct
import sys
import tempfile
import zlib
from pathlib import Path
from typing import Dict, List, Optional, Tuple

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

MAGIC              = b"PAC\x00"
HEADER_EXTRA       = 3          # u32 written at offset 8; always 3 in known files
HEADER_SIZE        = 12         # 4 (magic) + 4 (count) + 4 (extra)
ENTRY_SIZE         = 76         # bytes per file-table record
NAME_FIELD_SIZE    = 0x40       # 64 bytes; null-terminated Shift-JIS

LOG = logging.getLogger("pac")

# ---------------------------------------------------------------------------
# MSB Bit-Stream Primitives
# ---------------------------------------------------------------------------

class MsbBitWriter:
    """Accumulates bits MSB-first and emits bytes as they fill up."""

    __slots__ = ("_buf", "_bits", "_out")

    def __init__(self) -> None:
        self._buf  = 0        # pending bit accumulator
        self._bits = 0        # how many bits are pending
        self._out  = bytearray()

    def write(self, n: int, value: int) -> None:
        """Write the *n* least-significant bits of *value*, MSB first."""
        mask = (1 << n) - 1
        self._buf   = (self._buf << n) | (value & mask)
        self._bits += n
        while self._bits >= 8:
            self._bits -= 8
            self._out.append((self._buf >> self._bits) & 0xFF)

    def flush(self) -> bytes:
        """Return all accumulated bytes.  Any partial final byte is zero-padded."""
        result = bytearray(self._out)
        if self._bits > 0:
            result.append((self._buf << (8 - self._bits)) & 0xFF)
        return bytes(result)


class MsbBitReader:
    """Reads bits MSB-first from a bytes buffer.

    Matches the behaviour of ``io::MsbBitStream`` in arc_unpacker:
    * Reads 8 bits at a time from the underlying byte stream.
    * When the byte stream is exhausted, continues to supply zero bits
      (zero-padding), which is safe because the decoder always knows the
      exact output size and stops before hitting padding bits.
    """

    __slots__ = ("_data", "_pos", "_buf", "_avail")

    def __init__(self, data: bytes) -> None:
        self._data  = bytes(data)
        self._pos   = 0
        self._buf   = 0
        self._avail = 0   # bits currently held in _buf

    def read(self, n: int) -> int:
        """Return the next *n* bits as an unsigned integer."""
        while self._avail < n:
            if self._pos < len(self._data):
                self._buf   = (self._buf << 8) | self._data[self._pos]
                self._pos  += 1
                self._avail += 8
            else:
                # Zero-pad beyond end of stream
                self._buf   <<= 8
                self._avail  += 8
        mask         = (1 << n) - 1
        self._avail -= n
        return (self._buf >> self._avail) & mask


# ---------------------------------------------------------------------------
# Huffman Tree Construction
# ---------------------------------------------------------------------------

class _HNode:
    """A node in the Huffman tree used during construction."""

    __slots__ = ("freq", "value", "left", "right")

    def __init__(
        self,
        freq:  int,
        value: Optional[int] = None,
        left:  Optional["_HNode"] = None,
        right: Optional["_HNode"] = None,
    ) -> None:
        self.freq  = freq
        self.value = value   # int 0-255 for leaves; None for internal nodes
        self.left  = left
        self.right = right

    # ------------------------------------------------------------------
    # Total ordering for the min-heap.
    # Primary key : frequency (lower = higher priority).
    # Secondary   : leaves before internal nodes (deterministic ordering).
    # Tertiary    : byte value for leaves; object id for internal nodes.
    # ------------------------------------------------------------------
    def __lt__(self, other: "_HNode") -> bool:
        if self.freq != other.freq:
            return self.freq < other.freq
        sv = self.value  if self.value  is not None else (0x10000 + id(self))
        ov = other.value if other.value is not None else (0x10000 + id(other))
        return sv < ov

    def is_leaf(self) -> bool:
        return self.value is not None


def _build_huffman_tree(data: bytes) -> _HNode:
    """Build a Huffman tree from *data*, handling degenerate cases."""
    freq: List[int] = [0] * 256
    for b in data:
        freq[b] += 1

    heap: List[_HNode] = [
        _HNode(freq[i], value=i) for i in range(256) if freq[i] > 0
    ]
    heapq.heapify(heap)

    if not heap:
        # Completely empty input – should never happen in practice.
        LOG.warning("Empty input passed to Huffman builder; using fallback tree.")
        return _HNode(0, left=_HNode(0, value=0), right=_HNode(0, value=1))

    if len(heap) == 1:
        # Single unique symbol: add a zero-frequency sibling so the tree has
        # at least two leaves and can be encoded/decoded normally.
        only = heap[0]
        sibling_val = (only.value + 1) % 256
        sibling = _HNode(0, value=sibling_val)
        LOG.debug(
            "Only one unique byte (0x%02X); added dummy sibling 0x%02X.",
            only.value, sibling_val,
        )
        return _HNode(only.freq, left=only, right=sibling)

    while len(heap) > 1:
        left  = heapq.heappop(heap)
        right = heapq.heappop(heap)
        heapq.heappush(heap, _HNode(left.freq + right.freq, left=left, right=right))

    return heap[0]


def _collect_codes(node: _HNode, prefix: str, codes: Dict[int, str]) -> None:
    """Populate *codes* with the Huffman bit-string for each leaf."""
    if node.is_leaf():
        # Degenerate single-leaf tree: assign code "0" to avoid empty prefix.
        codes[node.value] = prefix if prefix else "0"
    else:
        assert node.left  is not None
        assert node.right is not None
        _collect_codes(node.left,  prefix + "0", codes)
        _collect_codes(node.right, prefix + "1", codes)


def _serialize_tree(node: _HNode, writer: MsbBitWriter) -> None:
    """Write the Huffman tree into *writer* in the pre-order format expected
    by ``init_huffman()`` in pac_archive_decoder.cc.

        internal node → bit 1, then left subtree, then right subtree
        leaf          → bit 0, then 8-bit byte value

    The decoder numbers internal nodes 256, 257, … in the order it
    encounters them (same as the pre-order traversal produced here).
    """
    if node.is_leaf():
        writer.write(1, 0)
        writer.write(8, node.value)  # type: ignore[arg-type]
    else:
        writer.write(1, 1)
        _serialize_tree(node.left,  writer)   # type: ignore[arg-type]
        _serialize_tree(node.right, writer)   # type: ignore[arg-type]


def huffman_encode(data: bytes) -> bytes:
    """Return the Huffman-encoded representation of *data*.

    Layout of the returned bit-stream (MSB-first, zero-padded to byte boundary):
        [tree serialisation] [encoded bytes]

    This is the inverse of ``decompress_table()`` in pac_archive_decoder.cc.
    """
    tree  = _build_huffman_tree(data)
    codes: Dict[int, str] = {}
    _collect_codes(tree, "", codes)

    writer = MsbBitWriter()
    _serialize_tree(tree, writer)

    for byte_val in data:
        code = codes[byte_val]
        for ch in code:
            writer.write(1, int(ch))

    return writer.flush()


# ---------------------------------------------------------------------------
# Huffman Decode (matching init_huffman from pac_archive_decoder.cc)
# ---------------------------------------------------------------------------

def _huffman_init_tree(
    reader: MsbBitReader,
    nodes:  List[List[int]],
    pos:    List[int],
) -> int:
    """Recursive tree deserialisation matching init_huffman() exactly.

    ``nodes[0][n]`` = left child  of internal node *n*
    ``nodes[1][n]`` = right child of internal node *n*
    Returns the index of the (sub-)root: 0-255 for leaf, 256-511 for internal.
    """
    if reader.read(1):
        old_pos = pos[0]
        pos[0] += 1
        if old_pos < 511:
            nodes[0][old_pos] = _huffman_init_tree(reader, nodes, pos)
            nodes[1][old_pos] = _huffman_init_tree(reader, nodes, pos)
            return old_pos
        return -1
    return reader.read(8)


def huffman_decode(compressed: bytes, output_size: int) -> bytes:
    """Decode *compressed* into exactly *output_size* bytes.

    Mirrors ``decompress_table()`` in pac_archive_decoder.cc.
    """
    reader: MsbBitReader = MsbBitReader(compressed)
    nodes:  List[List[int]] = [[0] * 512, [0] * 512]
    pos:    List[int] = [256]

    root = _huffman_init_tree(reader, nodes, pos)

    output = bytearray(output_size)
    for i in range(output_size):
        p = root
        while 256 <= p <= 511:
            p = nodes[reader.read(1)][p]
        output[i] = p

    return bytes(output)


# ---------------------------------------------------------------------------
# PAC Table Parsing / Serialisation
# ---------------------------------------------------------------------------

def _parse_table(raw: bytes, file_count: int) -> List[dict]:
    """Parse the raw (decoded) file table into a list of entry dicts."""
    if len(raw) < file_count * ENTRY_SIZE:
        raise ValueError(
            f"Table too short: expected {file_count * ENTRY_SIZE} bytes, "
            f"got {len(raw)}"
        )

    entries: List[dict] = []
    for i in range(file_count):
        base = i * ENTRY_SIZE
        name_field = raw[base : base + NAME_FIELD_SIZE]
        null_at = name_field.find(0)
        name_bytes = name_field[:null_at] if null_at >= 0 else name_field

        try:
            name = name_bytes.decode("shift_jis")
        except UnicodeDecodeError:
            name = name_bytes.decode("latin-1")
            LOG.warning("Entry %d: Shift-JIS decode failed; using latin-1.", i)

        offset, size_orig, size_comp = struct.unpack_from(
            "<III", raw, base + NAME_FIELD_SIZE
        )

        entries.append(
            {
                "index":          i,
                "name":           name,
                "name_bytes_hex": name_bytes.hex(),
                "offset":         offset,
                "size_orig":      size_orig,
                "size_comp":      size_comp,
                "is_compressed":  size_orig != size_comp,
            }
        )
    return entries


def _build_table_bytes(entries: List[dict]) -> bytes:
    """Serialise a list of entry dicts back into the raw 76-byte-per-entry format."""
    buf = bytearray()
    for e in entries:
        name_raw = bytes.fromhex(e["name_bytes_hex"])
        # Null-terminate and pad/truncate to exactly NAME_FIELD_SIZE bytes.
        padded = (name_raw + b"\x00" * NAME_FIELD_SIZE)[:NAME_FIELD_SIZE]
        buf += padded
        buf += struct.pack("<III", e["offset"], e["size_orig"], e["size_comp"])
    return bytes(buf)


# ---------------------------------------------------------------------------
# PAC Read / Write
# ---------------------------------------------------------------------------

def read_pac(pac_path: str) -> Tuple[List[dict], bytes, int]:
    """Open *pac_path* and return (entries, full_data, header_extra).

    ``full_data`` is the raw bytes of the entire file (kept in RAM so
    callers can slice file blobs without seeking).
    """
    LOG.info("Reading '%s' …", pac_path)
    with open(pac_path, "rb") as fh:
        data = fh.read()

    if data[:4] != MAGIC:
        raise ValueError(f"Not a PAC file (magic={data[:4]!r})")

    file_count   = struct.unpack_from("<I", data, 4)[0]
    header_extra = struct.unpack_from("<I", data, 8)[0]
    LOG.debug("file_count=%d  header_extra=%d", file_count, header_extra)

    size_comp_table = struct.unpack_from("<I", data, len(data) - 4)[0]
    table_start     = len(data) - 4 - size_comp_table
    LOG.debug(
        "Compressed table: offset=%d  size=%d  table_size_orig=%d",
        table_start, size_comp_table, file_count * ENTRY_SIZE,
    )

    # XOR-undo, then Huffman-decode the file table.
    xored     = bytearray(data[table_start : len(data) - 4])
    n         = len(xored)
    for i in range(n):
        xored[i] ^= 0xFF

    table_raw = huffman_decode(bytes(xored), file_count * ENTRY_SIZE)
    entries   = _parse_table(table_raw, file_count)

    return entries, data, header_extra


def extract_file(entry: dict, pac_data: bytes) -> bytes:
    """Extract and (if necessary) decompress a single file from PAC data."""
    offset    = entry["offset"]
    size_comp = entry["size_comp"]
    size_orig = entry["size_orig"]

    blob = pac_data[offset : offset + size_comp]
    if entry["is_compressed"]:
        blob = zlib.decompress(blob)

    if len(blob) != size_orig:
        LOG.warning(
            "  Size mismatch for '%s': expected %d got %d",
            entry["name"], size_orig, len(blob),
        )
    return blob


# ---------------------------------------------------------------------------
# Unpack Command
# ---------------------------------------------------------------------------

def unpack(pac_path: str, output_dir: str) -> None:
    """Extract all files from *pac_path* into *output_dir*.

    Writes a ``manifest.json`` alongside the extracted files so that
    ``repack`` can reconstruct the PAC byte-accurately (same filenames,
    same compression decisions).
    """
    entries, pac_data, header_extra = read_pac(pac_path)
    out = Path(output_dir)
    out.mkdir(parents=True, exist_ok=True)

    LOG.info("Extracting %d file(s) to '%s' …", len(entries), output_dir)

    for e in entries:
        name       = e["name"]
        size_orig  = e["size_orig"]
        size_comp  = e["size_comp"]
        is_comp    = e["is_compressed"]

        LOG.info(
            "[%02d] %-50s  offset=%9d  orig=%9d  comp=%9d  zlib=%s",
            e["index"], repr(name), e["offset"], size_orig, size_comp, is_comp,
        )

        blob      = extract_file(e, pac_data)
        file_path = out / name

        # Safety guard: prevent path traversal.
        resolved = file_path.resolve()
        if not str(resolved).startswith(str(out.resolve())):
            LOG.error("  Skipping '%s' – path traversal detected!", name)
            continue

        file_path.parent.mkdir(parents=True, exist_ok=True)
        file_path.write_bytes(blob)
        LOG.info("  → %s  (%d bytes written)", file_path, len(blob))

    # Drop internal 'index' key before saving manifest.
    manifest_entries = [{k: v for k, v in e.items() if k != "index"} for e in entries]
    manifest = {
        "pac_path":      str(pac_path),
        "header_extra":  header_extra,
        "file_count":    len(entries),
        "entries":       manifest_entries,
    }
    manifest_path = out / "manifest.json"
    with open(manifest_path, "w", encoding="utf-8") as fh:
        json.dump(manifest, fh, ensure_ascii=False, indent=2)
    LOG.info("Manifest written → %s", manifest_path)


# ---------------------------------------------------------------------------
# Repack Command
# ---------------------------------------------------------------------------

def repack(input_dir: str, output_pac: str) -> None:
    """Repack the directory produced by ``unpack`` back into a PAC file.

    The ``manifest.json`` inside *input_dir* drives:
    * file order
    * original Shift-JIS filenames (stored verbatim in the table)
    * per-file compression decision

    If a file's compressed size would exceed its original size, the file
    is stored raw (size_comp == size_orig) regardless of the manifest flag.
    """
    inp = Path(input_dir)
    manifest_path = inp / "manifest.json"
    if not manifest_path.exists():
        raise FileNotFoundError(
            f"manifest.json not found in '{input_dir}'. "
            "Run 'unpack' first to generate it."
        )

    with open(manifest_path, "r", encoding="utf-8") as fh:
        manifest = json.load(fh)

    header_extra = manifest.get("header_extra", HEADER_EXTRA)
    source_entries: List[dict] = manifest["entries"]

    # If the manifest records the original PAC path, attempt to load it so
    # we can reuse existing compressed blobs for files that are unchanged on
    # disk. Reusing the original compressed bytes preserves bitwise equality
    # and the original PAC size for unchanged files.
    orig_pac_entries: Optional[List[dict]] = None
    orig_pac_data: Optional[bytes] = None
    pac_path_cfg = manifest.get("pac_path")
    if pac_path_cfg:
        try:
            if Path(pac_path_cfg).exists():
                orig_pac_entries, orig_pac_data, _ = read_pac(pac_path_cfg)
        except Exception:
            LOG.debug("Could not read original PAC at %s; proceeding without reuse", pac_path_cfg)

    LOG.info("Repacking %d file(s) from '%s' …", len(source_entries), input_dir)

    file_blobs:   List[bytes] = []
    new_entries:  List[dict]  = []
    cur_offset = HEADER_SIZE   # first blob starts immediately after the 12-byte header

    for e in source_entries:
        name      = e["name"]
        file_path = inp / name

        if not file_path.exists():
            raise FileNotFoundError(
                f"File not found: '{file_path}'. "
                "Make sure all files from the manifest are present."
            )

        raw = file_path.read_bytes()
        size_orig = len(raw)

        # If we have the original PAC data, and the on-disk raw content matches
        # the original extracted blob, reuse the original compressed bytes
        # directly instead of recompressing. This preserves the original
        # compressed size and keeps repacked PACs bitwise closer to originals.
        if orig_pac_entries is not None and orig_pac_data is not None:
            match = next((x for x in orig_pac_entries if x["name"] == name), None)
            if match is not None:
                orig_blob = extract_file(match, orig_pac_data)
                if orig_blob == raw:
                    comp_start = match["offset"]
                    comp_end   = comp_start + match["size_comp"]
                    blob       = orig_pac_data[comp_start:comp_end]
                    size_comp  = match["size_comp"]
                    LOG.info("  Reusing original compressed blob for %r (size=%d)", name, size_comp)

                    new_entries.append(
                        {
                            "name":           name,
                            "name_bytes_hex": e["name_bytes_hex"],
                            "offset":         cur_offset,
                            "size_orig":      size_orig,
                            "size_comp":      size_comp,
                            "is_compressed":  size_comp != size_orig,
                        }
                    )
                    file_blobs.append(blob)
                    cur_offset += size_comp
                    continue

        if e["is_compressed"]:
            comp = zlib.compress(raw, level=9)
            if len(comp) < size_orig:
                blob      = comp
                size_comp = len(comp)
            else:
                # Compression made it larger – store raw.
                LOG.debug("  '%s': compressed ≥ raw; storing uncompressed.", name)
                blob      = raw
                size_comp = size_orig
        else:
            blob      = raw
            size_comp = size_orig

        LOG.info(
            "  %-50s  orig=%9d  comp=%9d  zlib=%s",
            repr(name), size_orig, size_comp, size_comp != size_orig,
        )

        new_entries.append(
            {
                "name":           name,
                "name_bytes_hex": e["name_bytes_hex"],
                "offset":         cur_offset,
                "size_orig":      size_orig,
                "size_comp":      size_comp,
                "is_compressed":  size_comp != size_orig,
            }
        )
        file_blobs.append(blob)
        cur_offset += size_comp

    # Build file table → Huffman-encode → XOR 0xFF.
    table_raw  = _build_table_bytes(new_entries)
    LOG.debug("Raw table: %d bytes", len(table_raw))

    encoded    = huffman_encode(table_raw)
    LOG.debug("Huffman-encoded table: %d bytes", len(encoded))

    xored      = bytes(b ^ 0xFF for b in encoded)

    # Write the PAC.
    out_path = Path(output_pac)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    with open(out_path, "wb") as fh:
        fh.write(MAGIC)
        fh.write(struct.pack("<I", len(new_entries)))   # file_count
        fh.write(struct.pack("<I", header_extra))        # extra field (always 3)
        for blob in file_blobs:
            fh.write(blob)
        fh.write(xored)
        fh.write(struct.pack("<I", len(xored)))          # size_comp of table

    total = out_path.stat().st_size
    LOG.info("Written '%s'  (%d bytes total)", out_path, total)


# ---------------------------------------------------------------------------
# Self-Tests
# ---------------------------------------------------------------------------

def _decode_pac_table_raw(pac_data: bytes) -> Tuple[int, List[dict]]:
    """Helper: decode the file table from *pac_data* and return (file_count, entries)."""
    file_count      = struct.unpack_from("<I", pac_data, 4)[0]
    size_comp_table = struct.unpack_from("<I", pac_data, len(pac_data) - 4)[0]
    table_start     = len(pac_data) - 4 - size_comp_table

    xored = bytearray(pac_data[table_start : len(pac_data) - 4])
    for i in range(len(xored)):
        xored[i] ^= 0xFF

    table_raw = huffman_decode(bytes(xored), file_count * ENTRY_SIZE)
    return file_count, _parse_table(table_raw, file_count)


def run_tests(pac_path: str) -> bool:
    """Comprehensive round-trip test suite against *pac_path*.

    Tests performed
    ---------------
    1.  Bit-stream write / read symmetry (MsbBitWriter ↔ MsbBitReader).
    2.  Huffman encode / decode round-trip on synthetic data.
    3.  Huffman encode / decode round-trip on the actual PAC table bytes.
    4.  Full unpack: all files decompress without error and match recorded sizes.
    5.  Content verification: extracted files match bytes from in-PAC blobs.
    6.  Full repack: repacked PAC passes unpack without errors.
    7.  Round-trip content equality: every file identical before/after repack.
    """
    print(f"\n{'='*64}")
    print(f"  MinatoSoft PAC Test Suite")
    print(f"  Target: {pac_path}")
    print(f"{'='*64}\n")

    passed = 0
    failed = 0

    def ok(name: str) -> None:
        nonlocal passed
        passed += 1
        print(f"  [PASS] {name}")

    def fail(name: str, reason: str) -> None:
        nonlocal failed
        failed += 1
        print(f"  [FAIL] {name}: {reason}")

    # ------------------------------------------------------------------
    # Test 1: MsbBitWriter / MsbBitReader symmetry
    # ------------------------------------------------------------------
    name = "MSB bit-stream write→read symmetry"
    try:
        import random
        rng = random.Random(42)
        for trial in range(200):
            w = MsbBitWriter()
            expected: List[int] = []
            for _ in range(rng.randint(1, 40)):
                n = rng.randint(1, 16)
                v = rng.randint(0, (1 << n) - 1)
                w.write(n, v)
                expected.append((n, v))

            raw = w.flush()
            r   = MsbBitReader(raw)
            for n, v in expected:
                got = r.read(n)
                assert got == v, f"Trial {trial}: wrote {v} ({n}b), read {got}"
        ok(name)
    except AssertionError as exc:
        fail(name, str(exc))

    # ------------------------------------------------------------------
    # Test 2: Huffman encode / decode – synthetic data
    # ------------------------------------------------------------------
    name = "Huffman encode→decode (synthetic)"
    try:
        cases = [
            b"",                                         # empty
            bytes([0x42]),                               # single byte
            bytes([0xAB] * 100),                         # single unique value
            bytes(range(256)),                           # all byte values
            b"hello world! " * 200,                      # ASCII repetition
            bytes(range(256)) * 4 + b"PAC\x00" * 20,    # mixed
        ]
        for i, data in enumerate(cases):
            if not data:
                continue  # zero-length is trivially fine; skip encoding
            enc = huffman_encode(data)
            dec = huffman_decode(enc, len(data))
            assert dec == data, f"Case {i}: round-trip mismatch"
        ok(name)
    except AssertionError as exc:
        fail(name, str(exc))

    # ------------------------------------------------------------------
    # Test 3: Huffman round-trip on the real PAC table
    # ------------------------------------------------------------------
    name = "Huffman round-trip on real PAC table"
    try:
        with open(pac_path, "rb") as fh:
            pac_data = fh.read()

        if pac_data[:4] != MAGIC:
            fail(name, "file is not a PAC archive")
        else:
            file_count      = struct.unpack_from("<I", pac_data, 4)[0]
            size_comp_table = struct.unpack_from("<I", pac_data, len(pac_data) - 4)[0]
            table_start     = len(pac_data) - 4 - size_comp_table

            xored = bytearray(pac_data[table_start : len(pac_data) - 4])
            for i in range(len(xored)):
                xored[i] ^= 0xFF

            table_raw  = huffman_decode(bytes(xored), file_count * ENTRY_SIZE)
            re_encoded = huffman_encode(table_raw)
            re_decoded = huffman_decode(re_encoded, len(table_raw))

            assert re_decoded == table_raw, "Re-decoded table differs from original"
            assert len(re_encoded) == size_comp_table, (
                f"Re-encoded size {len(re_encoded)} ≠ original {size_comp_table}"
            )
            ok(name)
    except AssertionError as exc:
        fail(name, str(exc))

    # ------------------------------------------------------------------
    # Tests 4 & 5: Full unpack + content verification
    # ------------------------------------------------------------------
    with tempfile.TemporaryDirectory(prefix="pac_test_") as tmp:
        unpack_dir = os.path.join(tmp, "unpacked")

        name = "Full unpack (all files, no error)"
        try:
            entries, pac_data, _ = read_pac(pac_path)
            for e in entries:
                blob = extract_file(e, pac_data)
                assert len(blob) == e["size_orig"], (
                    f"'{e['name']}': size {len(blob)} ≠ {e['size_orig']}"
                )
            ok(name)
        except Exception as exc:
            fail(name, str(exc))
            entries = []
            pac_data = b""

        name = "Extracted file content matches in-PAC blob"
        try:
            unpack(pac_path, unpack_dir)
            for e in entries:
                disk_path = os.path.join(unpack_dir, e["name"])
                assert os.path.exists(disk_path), f"Missing: {e['name']}"
                disk_data = Path(disk_path).read_bytes()
                blob      = extract_file(e, pac_data)
                assert disk_data == blob, (
                    f"'{e['name']}': disk content differs from in-PAC data"
                )
            ok(name)
        except AssertionError as exc:
            fail(name, str(exc))
        except Exception as exc:
            fail(name, f"Unexpected error: {exc}")

        # ------------------------------------------------------------------
        # Test 6: Repack produces a parseable PAC
        # ------------------------------------------------------------------
        repacked_pac = os.path.join(tmp, "repacked.pac")

        name = "Repack produces a valid PAC (parseable)"
        try:
            repack(unpack_dir, repacked_pac)
            assert os.path.exists(repacked_pac), "Output file not created"
            with open(repacked_pac, "rb") as fh:
                rpdata = fh.read()
            assert rpdata[:4] == MAGIC, "Repacked magic mismatch"
            rp_count, rp_entries = _decode_pac_table_raw(rpdata)
            assert rp_count == len(entries), (
                f"File count: expected {len(entries)}, got {rp_count}"
            )
            ok(name)
        except AssertionError as exc:
            fail(name, str(exc))
        except Exception as exc:
            fail(name, f"Unexpected error: {exc}")

        # ------------------------------------------------------------------
        # Test 7: Round-trip content equality
        # ------------------------------------------------------------------
        name = "Round-trip content equality (unpack → repack → unpack)"
        try:
            unpack2_dir = os.path.join(tmp, "unpacked2")
            with open(repacked_pac, "rb") as fh:
                rp_data = fh.read()
            rp_entries, rp_pac_data, _ = read_pac(repacked_pac)

            errors: List[str] = []
            for orig_e, rp_e in zip(entries, rp_entries):
                # Name should be identical.
                if orig_e["name"] != rp_e["name"]:
                    errors.append(
                        f"Name mismatch: {orig_e['name']!r} vs {rp_e['name']!r}"
                    )
                    continue
                # Content should be identical.
                orig_blob = extract_file(orig_e, pac_data)
                rp_blob   = extract_file(rp_e,   rp_pac_data)
                if orig_blob != rp_blob:
                    errors.append(f"Content mismatch: '{orig_e['name']}'")

            if errors:
                fail(name, "; ".join(errors))
            else:
                ok(name)
        except Exception as exc:
            fail(name, f"Unexpected error: {exc}")

    # ------------------------------------------------------------------
    # Summary
    # ------------------------------------------------------------------
    total = passed + failed
    print(f"\n{'='*64}")
    print(f"  Results: {passed}/{total} passed, {failed} failed")
    print(f"{'='*64}\n")

    return failed == 0


# ---------------------------------------------------------------------------
# CLI Entry Point
# ---------------------------------------------------------------------------

def _build_arg_parser() -> argparse.ArgumentParser:
    ap = argparse.ArgumentParser(
        prog="minatosoft_pac",
        description=(
            "MinatoSoft PAC archive packer / unpacker.\n\n"
            "Supports the PAC format used by MinatoSoft titles "
            "(e.g. Majikoi Steam).\n\n"
            "Commands:\n"
            "  unpack  input.pac   output_dir/   – extract all files\n"
            "  repack  input_dir/  output.pac    – pack directory back to PAC\n"
            "  test    script.pac                – run self-tests\n"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    ap.add_argument(
        "--log-level",
        default="INFO",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        help="Verbosity (default: INFO)",
    )

    sub = ap.add_subparsers(dest="command", required=True)

    p_unpack = sub.add_parser("unpack", help="Extract a PAC archive")
    p_unpack.add_argument("input_pac",  help="Path to the .pac file")
    p_unpack.add_argument("output_dir", help="Destination directory for extracted files")

    p_repack = sub.add_parser("repack", help="Repack a directory into a PAC archive")
    p_repack.add_argument("input_dir",  help="Directory containing files + manifest.json")
    p_repack.add_argument("output_pac", help="Destination .pac file")

    p_test = sub.add_parser("test", help="Run round-trip self-tests against a PAC file")
    p_test.add_argument("pac_file", help="PAC file to use as test input")

    return ap


def main(argv: Optional[List[str]] = None) -> int:
    # Increase the recursion limit for very deep Huffman trees (up to 512 frames).
    sys.setrecursionlimit(max(sys.getrecursionlimit(), 4096))

    parser = _build_arg_parser()
    args   = parser.parse_args(argv)

    logging.basicConfig(
        level=getattr(logging, args.log_level),
        format="%(asctime)s  %(levelname)-8s  %(message)s",
        datefmt="%H:%M:%S",
    )

    try:
        if args.command == "unpack":
            unpack(args.input_pac, args.output_dir)

        elif args.command == "repack":
            repack(args.input_dir, args.output_pac)

        elif args.command == "test":
            success = run_tests(args.pac_file)
            return 0 if success else 1

    except FileNotFoundError as exc:
        LOG.error("File not found: %s", exc)
        return 2
    except ValueError as exc:
        LOG.error("Format error: %s", exc)
        return 3
    except Exception as exc:
        LOG.exception("Unexpected error: %s", exc)
        return 4

    return 0


if __name__ == "__main__":
    sys.exit(main())
