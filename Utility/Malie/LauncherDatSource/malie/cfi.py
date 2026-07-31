import struct

BitLen = 32
ValueMask = (1 << BitLen) - 1

def rotate_right(value, shift):
    value = (value >> shift) | (value << (BitLen - shift)) 
    return value & ValueMask

def rotate_left(value, shift):
    value = (value << shift) | (value >> (BitLen - shift)) 
    return value & ValueMask

class Cfi:
    def __init__(self, key: bytes, rotate_key: list[int]):
        self.key = key
        self.rotate_key = rotate_key
        self.block_len = 16

    def decrypt_block(self, block_offset: int, buffer: bytearray, index: int):
        block = buffer[index:index+16]
        
        # 1. Inverse of XOR (XOR with first byte)
        first = block[0]
        for i in range(1, 16):
            block[i] ^= first
            
        # 2. Inverse of rotation
        data32 = list(struct.unpack("<IIII", block))
        
        offset = block_offset >> 4
        
        k0 = rotate_right(self.rotate_key[0], self.key[offset & 0x1F] ^ 0xA5)
        data32[0] = rotate_right(data32[0] ^ k0, self.key[(offset + 12) & 0x1F] ^ 0xA5)
        
        k1 = rotate_left(self.rotate_key[1], self.key[(offset + 3) & 0x1F] ^ 0xA5)
        data32[1] = rotate_left(data32[1] ^ k1, self.key[(offset + 15) & 0x1F] ^ 0xA5)
        
        k2 = rotate_right(self.rotate_key[2], self.key[(offset + 6) & 0x1F] ^ 0xA5)
        data32[2] = rotate_right(data32[2] ^ k2, self.key[(offset - 14) & 0x1F] ^ 0xA5)
        
        k3 = rotate_left(self.rotate_key[3], self.key[(offset + 9) & 0x1F] ^ 0xA5)
        data32[3] = rotate_left(data32[3] ^ k3, self.key[(offset - 11) & 0x1F] ^ 0xA5)
        
        buffer[index:index+16] = struct.pack("<IIII", *data32)
