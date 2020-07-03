# -*- coding: utf-8 -*-

import sys, codecs, string, locale, os, array
import Stemmer	#It's Snowball stemmer: http://snowball.tartarus.org/wrappers/PyStemmer-1.1.0.tar.gz. Should be installed.
import platform
from django.utils.encoding import smart_str, smart_unicode	# Django.utils should be installed
# Also should be installed script pdf2txt.py and PdfMiner extension

# module global settings ---
endOfSentence = u'.!?' # simvoli konca predlozheniya
minTermLen = 3 # minimalnaya dlina tega
# --------------------------

# skips chars from endOfSentence
def f(str, char):
	if (char.isalpha() or char.isnumeric() or (char in endOfSentence)):
		str = str + char
	else:
		if str[-1:] != ' ': str = str + ' '
	return str

# doesn't skip chars from endOfSentence
def g(str, char):
	if (char.isalpha() or char.isnumeric()):
		str = str + char
	else:
		if str[-1:] != ' ': str = str + ' '
	return str

# removes all non-numerals and non-letters (optionally except possible line-endings)
def removePunctuation(str, removeEndOfSentence = True):
	if removeEndOfSentence:
		return reduce(g, str.strip(), u'')
	else:
		return reduce(f, str.strip(), u'')

def listToString(list):
	return reduce(lambda string, item: string + item, list, "")

# writes to fileOut a frequency dictionary made up from terms found in fileIn
def parseFile(fileIn, fileOut):
	sys = platform.system()
	flag = 0
	if sys == 'Windows':
		fIn = codecs.open(fileIn, 'r', 'cp1251')
	if sys == 'Linux':
		fIn = codecs.open(fileIn, 'r', 'utf-8')
		locale.setlocale(locale.LC_ALL, 'ru_RU.utf-8')
	print 'Parsing ' + fileIn + ' ...'
	words = {} # eto slovar, hranit pari slovo - chastota vstrechaemosti
	forms = {}
	stemmerEn = Stemmer.Stemmer('en') # objavlyaem funccii dlya stemminga
	stemmerRu = Stemmer.Stemmer('ru')
	for line in fIn:
		firstWord = 1
		for word in removePunctuation(line, False).split():
			if firstWord == 0:
				if word[:1].isupper():
					orig = removePunctuation(word, True).split()[0]
					word = removePunctuation(word.lower(), True).split()[0]
					word = listToString(stemmerEn.stemWords([word]))
					word = listToString(stemmerRu.stemWords([word]))
					if len(word) < minTermLen: continue
					if word in words.keys():
						words[word] = words[word] + 1 # uvelichivaem chastotu vstrechaemosti
					else:
						words[word] = 1
					# sohranyaem originalnuyu formu slova
					if word in forms.keys():
						if orig in forms[word].keys():
							forms[word][orig] = forms[word][orig] + 1
						else:
							forms[word][orig] = 1
					else:
						forms[word] = {}
						forms[word][orig] = 1
			firstWord = 0
			if word[-1:] in endOfSentence:
				firstWord = 1
	fIn.close() # zakruvaem vhodnoy file
	if flag == 1:
		os.remove(fileIn)
	if sys == 'Windows':
		fOut = codecs.open(fileOut, 'w', 'cp1251') # otkrivaem vihodnoy file
	if sys == 'Linux':
		fOut = codecs.open(fileOut, 'w', 'utf-8')
	for word in words.keys():
		fOut.write(word + ' ' + str(words[word]) + ' ')
		freq, w = 0, ""
		for s in forms[word].keys():
			if forms[word][s] > freq:
				w = s
				freq = forms[word][s]
		fOut.write(w + '\n')
	fOut.close()	
	""" for line in fIn:
		firstWord = 1
		for word in removePunctuation(line, False).split():
			if firstWord == 0:
				if word[:1].isupper():
					orig = removePunctuation(word, True).split()[0]

					word = removePunctuation(word.lower(), True).split()[0]
					word = listToString(stemmerEn.stemWords([word]))
					word = listToString(stemmerRu.stemWords([word]))
					if len(word) < minTermLen: continue
					if word in words.keys():
						words[word] = words[word] + 1
					else:
						words[word] = 1

					# saving original word form
					if word in forms.keys():
						if orig in forms[word].keys():
							forms[word][orig] = forms[word][orig] + 1
						else:
							forms[word][orig] = 1
					else:
						forms[word] = {}
						forms[word][orig] = 1
			firstWord = 0
			if word[-1:] in endOfSentence:
				firstWord = 1
	fIn.close()

	fOut = codecs.open(fileOut, 'w', 'utf-8')
	for word in words.keys():
		fOut.write(word + ' ' + str(words[word]) + ' ')
		freq, w = 0, ""
		for s in forms[word].keys():
			if forms[word][s] > freq:
				w = s
				freq = forms[word][s]
		fOut.write(w + '\n')
	fOut.close() """

def getTopTags(dicFile, num = 5):
	dic = codecs.open(dicFile, 'r', 'utf-8')
	terms = {}
	for line in dic:
		terms[line.split(' ')[2]] = int(line.split(' ')[1])
	res = sorted(terms, key = lambda x: terms[x], reverse = True)[0:num]
	for s in res: print s
	return res

def buildTermDictionaries(path, numOfPosts = 1005):
	for i in xrange(1, numOfPosts):
		pathIn = path + '/' + str(i) + '.txt'
		if os.access(pathIn, os.F_OK) == 1:
			parseFile(pathIn, path + '/' + str(i) + '.dic')
	parseFile(path + '/allPosts.txt', path + '/allPosts.dic')

def main(args):
	parseFile(args[1], args[1] + ".tag")

if __name__ == "__main__": main(sys.argv)
