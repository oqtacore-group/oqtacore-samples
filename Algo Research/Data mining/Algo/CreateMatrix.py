import sys, csv, codecs, TermParser, nearest, os
#from ctypes import cdll

def unicode_csv_reader(utf8_data, dialect=csv.excel, **kwargs):
    csv_reader = csv.reader(utf8_data, dialect=dialect, **kwargs)
    for row in csv_reader:
        yield [unicode(cell, 'utf-8') for cell in row]

def quote(str):
	return '"' + str.replace('"', "'") + '"'

def WaitForFile(filename):
	while (not os.path.exists(filename)):
		time.sleep(0.1)

# Arguments: files to be processed
# Creates: <files>.dic, all.dic, matrix.txt
def main(args):
	NGram = "N-gram.exe"
	allFiles = codecs.open('all.txt', 'w', 'utf-8')
	for file in args[1:]:
		TermParser.parseFile(file, file + ".dic")
		curFile = codecs.open(file, 'r', 'utf-8')
		for line in curFile:
			allFiles.write(line)
		curFile.close()
	allFiles.close()
	TermParser.parseFile('all.txt', "all.dic")
	WaitForFile("all.dic")
	####################################################################
	print "Generating matrix ...\n"
	matrix = codecs.open("matrix.txt", 'w', 'utf-8')
	for file in args[1:]:
		curDic = codecs.open(file + '.dic', 'r', 'utf-8')
		cur = {}
		allDic = codecs.open('all.dic', 'r', 'utf-8')
		for line in allDic:
			word = line.split()[0]
			cur[word] = 0
		allDic.close()
		toAdd = False;	#not to add zero lines
		for line in curDic:
			word = line.split()[0]
			count = line.split()[1]
			if cur.keys().count(word) > 0:
				cur[word] = count
				toAdd = True
		curDic.close()
		if toAdd:
			matrix.write(file)
			for word in cur.keys():
				matrix.write(str(cur[word]) + " ");
			matrix.write("\n");
	os.remove('all.txt')
	nearest.findSimilar()
	
if __name__ == "__main__": main(sys.argv)
