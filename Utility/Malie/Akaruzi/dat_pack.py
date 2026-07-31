# ------------------------------------------------------------
# https://github.com/satan53x/SExtractor/tree/main/tools/Malie
# 萓晁ｵ匁ｨ｡蝮・tqdm
# ------------------------------------------------------------
import sys
import os
from tkinter import filedialog
from encoder_cfi import EncoderCfi, getDatabaseCfi
from encoder_camellia import EncoderCamellia, getDatabaseCameliia

PackName = 'new.dat'
#ExpectHeader = None
# ExpectHeader = bytes.fromhex('5D C4 43 14 18 7B BA 5B') #蝨ｨ豁､螟・｡ｫ蜀吝次蛹・ｼ螟ｴ隨ｬ0x10~0x17蟄苓鰍・御ｸ堺ｸｺ遨ｺ譌ｶ莨夊・蜉ｨ蛹ｹ驟埼・鄂ｮ aa_ver
# 50 1D 09 DE BE 3C 5C 9E steamver
ExpectHeader = bytes.fromhex('50 1D 09 DE BE 3C 5C 9E')  
# ExpectHeader = bytes.fromhex('AC BF 54 63 AA 01 6E 32') kkk

GameType = '' #莉・惠ExpectHeader譌謨域慮菴ｿ逕ｨ
IfEncrypt = True
CheckPlain = [0] * 0x10 #髴隕∵｣譟･逧・・譁・CheckOffset = 0x10 #髴隕∵｣譟･逧・・譁・柄蠎ｦ

# ------------------------------------------------------------
#var
dirpath = ''
filenameList = [] 
content = []

DefaultPath = ''
BlockLen = 0x10
Signature = 'LIBP'.encode('cp932')

config = None
indexSection = []
offsetSection = []
fileSection = []
indexSeq = 0

# ------------------------------------------------------------
def pack():
	indexSection.clear()
	offsetSection.clear()
	fileSection.clear()
	#驕榊紙
	print('Indexing...')
	root = Index('', 0)
	indexSection.append(root)
	traverse(dirpath, root)
	#蜀吝・
	#head
	output = bytearray(0x10)
	output[0:4] = Signature
	output[4:8] = len(indexSection).to_bytes(4, byteorder='little')
	output[8:12] = len(offsetSection).to_bytes(4, byteorder='little')
	#index
	for index in indexSection:
		output.extend(index.data)
	#offset
	offsetAddr = len(output)
	for offset in offsetSection:
		offset.addr = offsetAddr
		output.extend(offset.data)
		offsetAddr = len(output)
	fillingAlign(output)
	#file
	fileAddr = len(output)
	fileStart = fileAddr
	for file in fileSection:
		#菫ｮ豁｣offset
		file.offset.set(fileAddr - fileStart, output)
		output.extend(file.data)
		fillingAlign(output)
		fileAddr = len(output)
	#逕滓・
	#test()
	global content
	if IfEncrypt:
		print(f'Encrypting... Each line\'s bytes: 0x{BlockLen:x}')
		output = encrypt(output)
		print('Encrypted.')
	content = [output]
	write()

def traverse(path, index):
	#譁・ｻｶ
	if os.path.isfile(path):
		file = File(path)
		index.set(0x1, file.seq, len(file.data))
		return
	# 闔ｷ蜿匁枚莉ｶ螟ｹ荳ｭ逧・園譛画枚莉ｶ蜥悟ｭ先枚莉ｶ螟ｹ
	children = os.listdir(path)
	children = sorted(children)
	if len(children) == 0:
		index.set(0, 0, 0)
		print('\033[31m譁・ｻｶ螟ｹ荳ｺ遨ｺ・喀033[0m', path)
		return
	#蜈亥頃菴・	indexList = []
	for name in children:
		#print('index:', name)
		childIndex = Index(name, len(indexSection))
		indexSection.append(childIndex)
		indexList.append(childIndex)
	#隶ｾ鄂ｮ蠖灘燕邏｢蠑・	index.set(0, indexList[0].indexSeq, len(indexList))
	#蜷主ｺ城″蜴・	for i, name in enumerate(children):
		pathChild = os.path.join(path, name)
		if os.path.isfile(pathChild): #譁・ｻｶ
			traverse(pathChild, indexList[i])
	for i, name in enumerate(children):
		pathChild = os.path.join(path, name)
		if os.path.isdir(pathChild): #逶ｮ蠖・			traverse(pathChild, indexList[i])
	return 

def fillingAlign(output):
	size = config['Align']
	remain = len(output) - len(output) // size * size 
	if remain == 0: return
	#蝪ｫ蜈・	bs = bytes([0x00] * (size - remain))
	output.extend(bs)

def encrypt(data, offset=0, printed=True):
	enc = config['Encoder'](config)
	enc.encryptAll(data, offset, printed)
	return data

# ------------------------------------------------------------
class Index():
	def __init__(self, name, indexSeq) -> None:
		self.data = bytearray(0x20)
		bs = name.encode('cp932')
		self.data[0:len(bs)] = bs
		self.indexSeq = indexSeq
	
	def set(self, flag, seq, count):
		self.data[0x16:0x18] = flag.to_bytes(2, byteorder='little')
		self.data[0x18:0x1C] = seq.to_bytes(4, byteorder='little')
		self.data[0x1C:0x20] = count.to_bytes(4, byteorder='little')

class Offset():
	def __init__(self) -> None:
		self.data = bytearray(4)
		self.addr = 0 #蝨ｨ蛹・㈹逧・慍蝮
	
	def set(self, fileAddr, output):
		i = fileAddr // 0x400 #蝗ｺ螳・		output[self.addr:self.addr+4] = i.to_bytes(4, byteorder='little')

class File():
	def __init__(self, path) -> None:
		fileOld = open(path, 'rb')
		self.data = fileOld.read()
		fileOld.close()
		self.name = os.path.basename(path)
		self.seq = len(fileSection)
		fileSection.append(self)
		self.offset = Offset()
		offsetSection.append(self.offset)

# ------------------------------------------------------------
def write():
	path = os.path.join(dirpath, '..')
	if not os.path.exists(path):
		os.makedirs(path)
	name = PackName
	filepath = os.path.join(path, name)
	fileNew = open(filepath, 'wb')
	fileNew.writelines(content)
	fileNew.close()
	print(f'Write done: {name}')

def listFiles(start_path):
	file_list = []
	for root, dirs, files in os.walk(start_path):
		for file in files:
			# 闔ｷ蜿也嶌蟇ｹ霍ｯ蠕・			relative_path = os.path.relpath(os.path.join(root, file), start_path)
			file_list.append(relative_path)
	return file_list 

def main():
	path = DefaultPath
	if len(sys.argv) < 2:
		path = filedialog.askdirectory(initialdir=path)
	else:
		path = sys.argv[1]
	global dirpath
	if os.path.isdir(path):
		initConfig()
		if not config: return
		dirpath = path
		files = listFiles(path)
		filenameList.extend(files)
		pack()

# ------------------------------------------------------------
def initConfig():
	global config
	databaseCfi = getDatabaseCfi()
	databaseCamellia = getDatabaseCameliia()
	if ExpectHeader:
		print('Try to find expect...')
		#譟･謇ｾcfi蜉蟇・		for i, c in databaseCfi.items():
			bs = bytearray(BlockLen)
			bs[0:16] = CheckPlain
			config = c
			bs = encrypt(bs, CheckOffset, False)
			if bs[0:len(ExpectHeader)] == ExpectHeader:
				print('Find expect config:', i)
				return
		#譟･謇ｾcamellia蜉蟇・		for i, c in databaseCamellia.items():
			bs = bytearray(BlockLen)
			bs[0:16] = CheckPlain
			config = c
			bs = encrypt(bs, CheckOffset, False)
			if bs[0:len(ExpectHeader)] == ExpectHeader:
				print('Find expect config:', i)
				return
		print('Cannot find expect ExpectHeader.')
	config = None
	if GameType in databaseCfi:
		config = databaseCfi[GameType]
		print('Find config by GameType:', GameType)
	elif GameType in databaseCamellia:
		config = databaseCamellia[GameType]
		print('Find config by GameType:', GameType)
	else:
		print('Cannot find expect GameType.')

def test():
	filepath = os.path.join(dirpath, 'tmp.dat')
	fileOld = open(filepath, 'rb')
	data = fileOld.read()
	fileOld.close()
	data = encrypt(bytearray(data), 0)
	global content
	content = [data]

main()
