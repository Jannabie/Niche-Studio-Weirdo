import core

def IFN():
    # IFN (int, expr_str, {jump})
    core.read_uint16(True)
    core.read_str(core.expr)
    core.read_jump()
    core.end()

def IFY():
    # IFY (int, expr_str, {jump})
    core.read_uint16(True)
    core.read_str(core.expr)
    core.read_jump()
    core.end()

def GOTO():
    # GOTO ({jump})
    core.read_jump()
    core.end()

def ONGOTO():
    # read_jump() reads uint32 (4 bytes). Buffer = N*4 + 1 trailing byte.
    # Read jump targets while at least 4 bytes remain, then consume trailing uint8.
    n = core.read_uint8(False)
    for _ in range(n):
        core.read_jump()
    core.end()

def JUMP():
    # JUMP (int, file_str, {jump})
    core.read_uint16(True)
    file = core.read_str(core.expr)
    if core.can_read():
        core.read_jump(file)
    core.end()
