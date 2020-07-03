// MatrixBuilder.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <unordered_map>
#include <string>
#include <vector>
#include <algorithm>

using namespace std;

bool simpleCmp(const pair<string, pair<int, int> >& lhs, const pair<string, pair<int, int> >& rhs)
{
	return lhs.second.first < rhs.second.first;
}

int main(int argc, char* argv[])
{
	char* dirNameEnd = strrchr(argv[1], '/');
	if(dirNameEnd == NULL)
	{
		dirNameEnd = strrchr(argv[1], '\\');
		if(dirNameEnd == NULL)	//local name used
			dirNameEnd =argv[1];
	}
	int dirNameLength = dirNameEnd -argv[1] + 1;
	char* dirName = (char*)malloc(dirNameLength + 1);
	strncpy(dirName, argv[1], dirNameLength);
	FILE* fNames = fopen(argv[1], "r");
	//open every file and read its content to build dictionary of terms
#define BUFFER_SIZE 256
	char buffer[BUFFER_SIZE];
	char buf2[BUFFER_SIZE];
	strncpy(buf2, dirName, dirNameLength);
	int id = 0, popularity;
	unordered_map<string, pair<int,int> > dict;//<term, <popularity, docCount> >
	string s;
	dict.rehash(8388608);
	int fileCount = 0;
	while(!feof(fNames))
	{
		if(fgets(buffer, BUFFER_SIZE, fNames)== NULL)
			break;
//		strcat(buffer, ".dic.win");
		strcpy(buffer +strlen(buffer) - 1, ".dic.win");//cutting off carriage return
		strcpy(buf2 + dirNameLength, buffer);
		//sprintf(buffer, "%s.dic.win", buffer);
		FILE* dicFile = fopen(buf2, "r");
		while(!feof(dicFile))
		{
			fscanf(dicFile, "%s\t%d", buffer, &popularity);
			s.assign(buffer);
			auto it = dict.find(s);
			if(it == dict.end())
				dict.insert(make_pair(s, make_pair(popularity, 1)));
			else
			{
				it->second.first+= popularity;
				it->second.second += 1;
			}
		}
		fclose(dicFile);
		fileCount ++;
		printf("%d ", fileCount);
	}
	fclose(fNames);
	printf("\n Built Dic");
	vector<pair<string, pair<int, int> > > v;
	v.reserve(dict.size());
	for(auto it = dict.begin(); it != dict.end(); ++it)
	{
		v.push_back(*it);
	}
	sort(v.begin(), v.end(), simpleCmp);
	FILE* f = fopen("temp.txt", "w");
	for(int i = 0; i < v.size(); i+=1000)
		fprintf(f, "%s - <%ld, %ld>\n", v[i].first.data(), v[i].second.first, v[i].second.second);
	fclose(f);
	unordered_map<string, int> finDict;
	finDict.rehash(8388608);
	int counter = 0;
	FILE* fTerm = fopen("term", "w");
	for(auto it = v.begin(); it != v.end(); ++it)
	{
		if(it->second.first > 3 || it->second.second > 1 )
		{
			finDict.insert(make_pair(it->first, counter));
			counter ++;
			fprintf(fTerm, "%s\n", it->first.data());
		}
	}
	fclose(fTerm);
	printf("Counter = %d\n", counter);
	//now building sparse matrix file
	fNames = fopen(argv[1], "r");
	vector<int> cur;
	cur.resize(counter);
	int ind;
	fileCount = 0;
	strcpy(buf2 + dirNameLength, "matrix.csv");
	FILE* csvMatrix = fopen(buf2, "w");
	strcpy(buf2 + dirNameLength, "matrix");
	FILE* matrix = fopen(buf2, "w");
	strcpy(buf2 + dirNameLength, "sepTerms.txt");
	FILE* sepTerms = fopen(buf2, "w");
	while(!feof(fNames))
	{
		if(fgets(buffer, BUFFER_SIZE, fNames)== NULL)
			break;
		strcpy(buffer +strlen(buffer) - 1, ".dic.win");//cutting off carriage return
		strcpy(buf2 + dirNameLength, buffer);
		FILE* dicFile = fopen(buf2, "r");
		cur.assign(counter, 0);
		while(!feof(dicFile))
		{
			fscanf(dicFile, "%s\t%d", buffer, &popularity);
			s.assign(buffer);
			auto it = finDict.find(s);
			if(it != finDict.end())
			{
				ind = it->second;
				cur[ind] = popularity;
				fprintf(csvMatrix, "%d, %d, %d\n", fileCount, ind, popularity);
			}
			else
			{
				//rare term, let's write it separately for future special processing
				auto it2 = dict.find(s);
				fprintf(sepTerms, "%d\t%s\t%d\t%d\n", fileCount, s.data(), it2->second.first, it2->second.second);
			}
		}
		fclose(dicFile);
		for(auto it = cur.begin(); it != cur.end(); ++it)
		{
			fprintf(matrix, "%d ", *it);
		}
		fprintf(	matrix, "\n");
		fileCount ++;
		printf("%d ", fileCount);
	}
	fclose(fNames);
	fclose(csvMatrix);
	fclose(matrix);
	fclose(sepTerms);

	return 0;
}

