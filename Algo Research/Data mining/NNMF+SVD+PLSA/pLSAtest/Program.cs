/*
Copyright 2011, Andrew Polar

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */

//This is simple test of efficiency of Probabilistic Latent Semantic Analysis prepared by Andrew Polar.
//Domains: EzCodeSample.com, SemanticSearchArt.com

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace pLSAtest
{
	class Program
	{
		static StopWordFilter stopWordFilter = new StopWordFilter();
		static EnglishStemmer englishStemmer = new EnglishStemmer();
		static HashTableDictionary dictionary = new HashTableDictionary();
		static System.Text.Encoding enc = System.Text.Encoding.Default;
		static float[,] U = null;
		static int URows = 0;
		static int UCols = 0;
		static float[,] V = null;
		static int VRows = 0;
		static int VCols = 0;
		static float[,] DDist = null;
        static string dataFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Smart Tagger\\data");
		//The next property is used to reduce the dimensionality in SVD.
		//It tells to ignore singular values that are below the threshold
		//relativley to original. For example if it is 0.1, the value S[n] 
		//will be set to 0.0 if S[n]/S[0] < 0.1.
		static double m_singularNumbersThreashold = 0.04;
		static float[,] m_initialApproximation;
		static int nCategories = 1;
		static float[][] data = null;
		static bool bUsePLSA = true;
		static byte[] charFilter = {
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  8
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  16
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  24
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  32
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  40
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  48
			0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, //numbers  56
			0x38, 0x39, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //2 numbers and spaces  64
			0x20, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, //upper case to lower case 72
			0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, //upper case to lower case 80
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, //upper case to lower case 88
			0x78, 0x79, 0x7A, 0x20, 0x20, 0x20, 0x20, 0x20, //96
			0x20, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, //this must be lower case 104
			0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, //lower case 112
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, //120
			0x78, 0x79, 0x7A, 0x20, 0x20, 0x20, 0x20, 0x20, //128
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //136
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //144
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, //152
			0x78, 0x79, 0x7A, 0x20, 0x20, 0x20, 0x20, 0x20, //160
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //168
			0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //176
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, //184
			0x78, 0x79, 0x7A, 0x20, 0x20, 0x20, 0x20, 0x20, //192
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7,	// А-З to lower
			0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF, // И-П to lower
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, // Р-Ч to lower
			0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF, // Ш-Я to lower
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7,	// а-з
			0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF, // и-п
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, // р-ч
			0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF, // ш-я
		};
		private const int nWordSize = 256;
		static byte[] word = new byte[nWordSize];
        private static void enumerateFiles(List<string> files, string[] foldersFiles, string extension)
        {
            foreach (string folderOrFile in foldersFiles)
            {
                if (System.IO.Directory.Exists(folderOrFile))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(folderOrFile);
                    FileInfo[] fiArray = dirInfo.GetFiles(extension, SearchOption.AllDirectories);
                    foreach (FileInfo fi in fiArray)
                    {
                        files.Add(fi.FullName);
                    }
                }
                else if (System.IO.File.Exists(folderOrFile))
                    files.Add(new FileInfo(folderOrFile).FullName);
            }
        }
		private static byte[] GetFileData(string fileName)
		{
			FileStream fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
			int length = (int)fStream.Length;
			byte[] data = new byte[length];
			int count;
			int sum = 0;
			while ((count = fStream.Read(data, sum, length - sum)) > 0)
			{
				sum += count;
			}
			fStream.Close();
			return data;
		}
		//everything is written to matrix as a stream of triples (docID, wordID, count)
		private static void processFiles(ref List<string> files)
		{
			string path = dataFolder;
			Directory.CreateDirectory(dataFolder);
			string fileList = Path.Combine(dataFolder, "ProcessedFilesList.txt");
			string docWordMatrix = Path.Combine(dataFolder, "DocWordMatrix.dat");
			if (File.Exists(fileList)) File.Delete(fileList);
			if (File.Exists(docWordMatrix)) File.Delete(docWordMatrix);
			StreamWriter fileListStream = new StreamWriter(fileList);
			BinaryWriter docWordStream = new BinaryWriter(File.Open(docWordMatrix, FileMode.Create));
			ArrayList wordCounter = new ArrayList();
			ArrayList termList = new ArrayList();
			int nFileCounter = 0;
			foreach (string file in files)
			{
				string fileTermName = Path.Combine(dataFolder, file+".term.txt");
                BinaryWriter fileTermWriter = new BinaryWriter(File.Open(fileTermName, FileMode.Create));
                fileTermWriter.Close();
				wordCounter.Clear();
				int nWordsSoFar = dictionary.GetNumberOfWords();
				wordCounter.Capacity = nWordsSoFar;
				for (int i = 0; i < nWordsSoFar; ++i)
				{
					wordCounter.Add(0);
				}
				byte[] data = GetFileData(file);
				int counter = 0;
				for (int i = 0; i < data.Length; ++i)
				{
					byte b = data[i];
					b = charFilter[b];
					if (b != 0x20 && counter < nWordSize)
					{
						word[counter] = b;
						++counter;
					}
					else
					{
						if (counter > 0)
						{
							string strWord = enc.GetString(word, 0, counter);
							if (!stopWordFilter.isThere(strWord, counter))
							{
								
								if (b < 128)
								{
									englishStemmer.SetCurrent(strWord);
									if (englishStemmer.Stem())
									{
										strWord = englishStemmer.GetCurrent();
									}
								}
								int nWordIndex = dictionary.GetWordIndex(strWord);
								//we check errors
								if (nWordIndex < 0)
								{
									if (nWordIndex == -1)
									{
										Console.WriteLine("Erorr: word = NULL");
										Environment.Exit(1);
									}
									if (nWordIndex == -2)
									{
										Console.WriteLine("Error: word length > 255");
										Environment.Exit(1);
									}
									if (nWordIndex == -3)
									{
										Console.WriteLine("Error: uknown");
										Environment.Exit(1);
									}
									if (nWordIndex == -4)
									{
										Console.WriteLine("Error: memory buffer for dictionary is too short");
										Environment.Exit(1);
									}
									if (nWordIndex == -5)
									{
										Console.WriteLine("Error: word length = 0");
										Environment.Exit(1);
									}
								}
								if (nWordIndex < nWordsSoFar)
								{
									int element = (int)wordCounter[nWordIndex];
									wordCounter[nWordIndex] = element + 1;
								}
								else
								{
									wordCounter.Add(1);
									termList.Add(strWord);
									++nWordsSoFar;
								}
							}
							counter = 0;
						} //word processed
					}

				}//file processed
				Console.WriteLine("File: " + file + ", words: " + dictionary.GetNumberOfWords() + ", size: " + dictionary.GetDictionarySize());
				fileListStream.WriteLine(nFileCounter.ToString() + " " + file);
				//StreamWriter termDocWriter = new StreamWriter(Path.Combine(path, String.Format("{0}.term", file)));
				int pos = 0;
				foreach (int x in wordCounter)
				{
					if (x > 0)
					{
						docWordStream.Write(nFileCounter);
						docWordStream.Write(pos);
						short value = (short)(x);
						docWordStream.Write(value);
					}
					++pos;
					
				}
				++nFileCounter;
			}//end foreach block, all files are processed
			fileListStream.Flush();
			fileListStream.Close();
			docWordStream.Flush();
			docWordStream.Close();
			dictionary.MakeVocabulary();
			string termDict = Path.Combine(dataFolder, "TermList.txt");
			if (File.Exists(termDict)) 
				File.Delete(termDict);
			dictionary.OutputVocabulary(termDict);


		}

		private static bool reformatMatrix()
		{
            string path = dataFolder;
			string matrixFile = Path.Combine(path, "DocWordMatrix.dat");
			if (!File.Exists(matrixFile))
			{
				Console.WriteLine("Matrix data file not found");
				return false;
			}
			FileInfo fi = new FileInfo(matrixFile);
			long nBytes = fi.Length;
			int nNonZeros = (int)(nBytes / 10);
			ArrayList list = new ArrayList();
			list.Clear();
			//посчитаем число строк и столбцов в матрице
			int nRows = 0;
			int nTotalCols = 0;
			using (FileStream stream = new FileStream(matrixFile, FileMode.Open))
			{
				int nCurrent = 0;
				int nCols = -1;
				using (BinaryReader reader = new BinaryReader(stream))
				{
					for (long i = 0; i < nNonZeros; ++i)
					{
						int nR = reader.ReadInt32();
						int nC = reader.ReadInt32();
						short sum = reader.ReadInt16();
						if (nC > nTotalCols) nTotalCols = nC;
						++nCols;
						if (nR != nCurrent)
						{
							++nRows;
							list.Add(nCols);
							nCurrent = nR;
							nCols = 0;
						}
					}
					list.Add(nCols + 1);
					reader.Close();
				}
				stream.Close();
			}
			++nRows;
			++nTotalCols;
			Console.WriteLine();
			string formattedMatrix = Path.Combine(path, "matrix");
			FileStream inStream = new FileStream(matrixFile, FileMode.Open);
			BinaryReader inReader = new BinaryReader(inStream);
			using (FileStream stream = new FileStream(formattedMatrix, FileMode.Create))
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(System.Net.IPAddress.HostToNetworkOrder(nTotalCols));
					writer.Write(System.Net.IPAddress.HostToNetworkOrder(nRows));
					writer.Write(System.Net.IPAddress.HostToNetworkOrder(nNonZeros));
					//в начале пишется хедер, затем для каждого докумена подряд пишутся число ненулевых термов в нем и пары (номер терма, частота в виде float)
					for (int k = 0; k < list.Count; ++k)
					{
						int nNonZeroCols = (int)list[k];
						writer.Write(System.Net.IPAddress.HostToNetworkOrder(nNonZeroCols));
						for (int j = 0; j < nNonZeroCols; ++j)
						{
							int nR = inReader.ReadInt32();
							int nC = inReader.ReadInt32();
							short sum = inReader.ReadInt16();
							float fSum = (float)(sum);
							writer.Write(System.Net.IPAddress.HostToNetworkOrder(nC));
							byte[] b = BitConverter.GetBytes(fSum);
							int x = BitConverter.ToInt32(b, 0);
							writer.Write(System.Net.IPAddress.HostToNetworkOrder(x));
						}
					}
					writer.Flush();
					writer.Close();
				}
				stream.Close();
			}
			inReader.Close();
			inStream.Close();
			return true;
		}

		private static bool prepareDocDocMatrix(string resultBlockName)
		{
			//read matrix
			int nRows = -1;
			int nCols = -1;
			string fUTfileName = resultBlockName + "-Ut";
			using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					nCols = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					nRows = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					reader.Close();
				}
				stream.Close();
			}
			if (nRows < 0 || nCols < 0) return false;
			U = new float[nRows, nCols];
			URows = nRows;
			UCols = nCols;
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					U[i, j] = (float)(0.0);
				}
			}
			using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int nRowsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					int nColsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					for (int i = 0; i < nRowsRead; ++i)
					{
						for (int j = 0; j < nColsRead; ++j)
						{
							int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
							byte[] b = BitConverter.GetBytes(nBuf);
							U[j, i] = BitConverter.ToSingle(b, 0);
						}
					}
					reader.Close();
				}
				stream.Close();
			}
			//end reading matrix
			//Read singular values
			int rank = 0;
			string singularValuesFile = resultBlockName + "-S";
			string line = string.Empty;
			System.IO.StreamReader file = new System.IO.StreamReader(singularValuesFile);
			line = file.ReadLine();
			if (line == null)
			{
				Console.WriteLine("Misformatted file: {0}", singularValuesFile);
				return false;
			}
			try
			{
				rank = Convert.ToInt32(line);
			}
			catch (Exception)
			{
				Console.WriteLine("Misformatted file: {0}", singularValuesFile);
				return false;
			}
			if (rank != nCols)
			{
				Console.WriteLine("Data mismatch");
				return false;
			}
			float[] singularValues = new float[rank];
			int cnt = 0;
			double maxSingularValue = 1.0;
			try
			{
				while ((line = file.ReadLine()) != null)
				{
					if (cnt == 0)
					{
						maxSingularValue = (float)(Convert.ToDouble(line));
					}
					singularValues[cnt] = (float)(Convert.ToDouble(line));
					if ((double)(singularValues[cnt]) / maxSingularValue < m_singularNumbersThreashold)
					{
						singularValues[cnt] = 0.0f;
					}
					++cnt;
				}
			}
			catch (Exception)
			{
				Console.WriteLine("Misformatted file: {0}", singularValues);
				return false;
			}
			file.Close();
			//end reading singular values
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					U[i, j] *= singularValues[j];
				}
			}
			return true;
		}

		private static bool prepareDocDocMatrix2(string resultBlockName)
		{
			//read matrix
			int nRows = -1;
			int nCols = -1;
			string fUTfileName = resultBlockName + "-Ut";
			using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					nCols = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					nRows = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					reader.Close();
				}
				stream.Close();
			}
			if (nRows < 0 || nCols < 0) return false;
			U = new float[nRows, nCols];
			URows = nRows;
			UCols = nCols;
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					U[i, j] = (float)(0.0);
				}
			}
			using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int nRowsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					int nColsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					for (int i = 0; i < nRowsRead; ++i)
					{
						for (int j = 0; j < nColsRead; ++j)
						{
							int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
							byte[] b = BitConverter.GetBytes(nBuf);
							U[j, i] = BitConverter.ToSingle(b, 0);
						}
					}
					reader.Close();
				}
				stream.Close();
			}
			//end reading matrix
			//Read singular values
			int rank = 0;
			string singularValuesFile = resultBlockName + "-S";
			string line = string.Empty;
			System.IO.StreamReader file = new System.IO.StreamReader(singularValuesFile);
			line = file.ReadLine();
			if (line == null)
			{
				Console.WriteLine("Misformatted file: {0}", singularValuesFile);
				return false;
			}
			try
			{
				rank = Convert.ToInt32(line);
			}
			catch (Exception)
			{
				Console.WriteLine("Misformatted file: {0}", singularValuesFile);
				return false;
			}
			if (rank != nCols)
			{
				Console.WriteLine("Data mismatch");
				return false;
			}
			float[] singularValues = new float[rank];
			int cnt = 0;
			double maxSingularValue = 1.0;
			try
			{
				while ((line = file.ReadLine()) != null)
				{
					if (cnt == 0)
					{
						maxSingularValue = (float)(Convert.ToDouble(line));
					}
					singularValues[cnt] = (float)(Convert.ToDouble(line));
					if ((double)(singularValues[cnt]) / maxSingularValue < m_singularNumbersThreashold)
					{
						singularValues[cnt] = 0.0f;
					}
					++cnt;
				}
			}
			catch (Exception)
			{
				Console.WriteLine("Misformatted file: {0}", singularValues);
				return false;
			}
			file.Close();
			//end reading singular values
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					U[i, j] *= (float)Math.Sqrt(singularValues[j]);
				}
			}
			return true;
		}


		private static bool prepareTermTermMatrix(string resultBlockName)
		{
			//read matrix
			int nRows = -1;
			int nCols = -1;
			string fUTfileName = resultBlockName + "-Vt";
			using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					nCols = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					nRows = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					reader.Close();
				}
				stream.Close();
			}
			if (nRows < 0 || nCols < 0) return false;
			V = new float[nRows, nCols];
			VRows = nRows;
			VCols = nCols;
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					V[i, j] = (float)(0.0);
				}
			}
			using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int nRowsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					int nColsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
					for (int i = 0; i < nRowsRead; ++i)
					{
						for (int j = 0; j < nColsRead; ++j)
						{
							int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
							byte[] b = BitConverter.GetBytes(nBuf);
							V[j, i] = BitConverter.ToSingle(b, 0);
						}
					}
					reader.Close();
				}
				stream.Close();
			}
			//end reading matrix
			//Read singular values
			int rank = 0;
			string singularValuesFile = resultBlockName + "-S";
			string line = string.Empty;
			System.IO.StreamReader file = new System.IO.StreamReader(singularValuesFile);
			line = file.ReadLine();
			if (line == null)
			{
				Console.WriteLine("Misformatted file: {0}", singularValuesFile);
				return false;
			}
			try
			{
				rank = Convert.ToInt32(line);
			}
			catch (Exception)
			{
				Console.WriteLine("Misformatted file: {0}", singularValuesFile);
				return false;
			}
			if (rank != nCols)
			{
				Console.WriteLine("Data mismatch");
				return false;
			}
			float[] singularValues = new float[rank];
			int cnt = 0;
			double maxSingularValue = 1.0;
			try
			{
				while ((line = file.ReadLine()) != null)
				{
					if (cnt == 0)
					{
						maxSingularValue = (float)(Convert.ToDouble(line));
					}
					singularValues[cnt] = (float)(Convert.ToDouble(line));
					if ((double)(singularValues[cnt]) / maxSingularValue < m_singularNumbersThreashold)
					{
						singularValues[cnt] = 0.0f;
					}
					++cnt;
				}
			}
			catch (Exception)
			{
				Console.WriteLine("Misformatted file: {0}", singularValues);
				return false;
			}
			file.Close();
			//end reading singular values
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					V[i, j] *= (float) Math.Sqrt(singularValues[j]);
				}
			}
			return true;
		}

		private static void computeRecognitionRatioForLSA() 
		{
            string path = dataFolder;
			string MatrixUt = Path.Combine(path, "result");
			//домножаем каждый документ на сингулярное значение
			prepareDocDocMatrix(MatrixUt);
			int[] basis = new int[8] {0,16,32,48,64,80,96,112};
			// цикл по всем документам
			for (int i = 0; i < URows; ++i)
			{
				for (int k = 0; k < 8; ++k)
				{
					double coeff = 0.0;
					double norm1 = 0.0;
					double norm2 = 0.0;
					for (int j = 0; j < UCols; ++j)
					{
						coeff += U[i, j] * U[basis[k], j];
						norm1 += U[i, j] * U[i, j];
						norm2 += U[basis[k], j] * U[basis[k], j];
					}
					coeff /= Math.Sqrt(norm1);
					coeff /= Math.Sqrt(norm2);
					//скалярное произведение текущего документа с документом-представителем класса
					m_initialApproximation[i, k] = (float)(coeff);
				}
				//Console.WriteLine();
			}
			int correct = 0;
			//цикл по всем документам
			for (int i = 0; i < 128; ++i)
			{
				float max = 0.0f;
				int nPos = 0;
				for (int j = 0; j < 8; ++j)
				{
					if (max < m_initialApproximation[i, j])
					{
						max = m_initialApproximation[i, j];
						nPos = j;
					}
				}
				//чекаем попадание в нужный класс
				if (i < 16) 
				{
					if (nPos == 0) ++correct;
				}
				else if (i < 32)
				{
					if (nPos == 1) ++correct;
				}
				else if (i < 48)
				{
					if (nPos == 2) ++correct;
				}
				else if (i < 64)
				{
					if (nPos == 3) ++correct;
				}
				else if (i < 80)
				{
					if (nPos == 4) ++correct;
				}
				else if (i < 96)
				{
					if (nPos == 5) ++correct;
				}
				else if (i < 112)
				{
					if (nPos == 6) ++correct;
				}
				else if (i < 128)
				{
					if (nPos == 7) ++correct;
				}
			}
			Console.WriteLine("Correct recognition ratio for LSA {0:f}", (double)(correct-8)/(double)(128-8));
		}

		private class ReverseComparer : IComparer<KeyValuePair<double, int>>
		{
			public int Compare(KeyValuePair<double, int> object1, KeyValuePair<double, int> object2)
			{
				double ret = (object2.Key - object1.Key);
				if (ret < 0)
					return -1;
				else if (ret > 0)
					return 1;
				else
					return 0;
			}
		}

		private static void computeTopDocumentsForLSA(int topLength)
		{
            string path = dataFolder;
			string MatrixUt = Path.Combine(path, "result");
			//домножаем каждый документ на сингулярное значение
			prepareDocDocMatrix2(MatrixUt);
			string namePath = Path.Combine(path, "ProcessedFilesList.txt");
			List<string> fNames = new List<string>();
			using (StreamReader sr = new StreamReader(namePath))
			{
				while (sr.Peek() != -1)
				{
					fNames.Add(sr.ReadLine());
				}
			}
			string distDocFile = Path.Combine(path, "DistDocFile.txt");
			List<KeyValuePair<double, int>> top = new List<KeyValuePair<double, int>>();
			StreamWriter resWriter = new StreamWriter(Path.Combine(path, "simFiles.txt"));
			StreamWriter distWriter = new StreamWriter(distDocFile);
			// цикл по всем документам
			for (int i = 0; i < URows; ++i)
			{
				top.Clear();
				for (int z = 0; z < URows; ++z)
				{
					double coeff = 0.0;
					double norm1 = 0.0;
					double norm2 = 0.0;
					for (int j = 0; j < UCols; ++j)
					{
						coeff += U[i, j] * U[z, j];
						norm1 += U[i, j] * U[i, j];
						norm2 += U[z, j] * U[z, j];
					}
					coeff /= Math.Sqrt(norm1);
					coeff /= Math.Sqrt(norm2);
					if (i != z)
						top.Add(new KeyValuePair<double, int>(coeff, z));
					distWriter.Write("{0} ", coeff);
				}
				distWriter.WriteLine();
				top.Sort(new ReverseComparer());
				IEnumerator<KeyValuePair<double, int>> en =  top.GetEnumerator();
				en.MoveNext();
				for(int k = 0; k < topLength; ++k)
				{
					resWriter.WriteLine(fNames[i] + "|" + fNames[en.Current.Value] + "|"+ en.Current.Key);
					en.MoveNext();
				}
				resWriter.WriteLine();
			}
			resWriter.Close();
			distWriter.Close();
		}

		private static void FitPLSAWithInitialApproximation()
		{
            string path = dataFolder;
			string MatrixUt = Path.Combine(path, "result");
			//домножаем каждый документ на сингулярное значение
			if (U == null)
				prepareDocDocMatrix2(MatrixUt);
			for (int i = 3; i < Math.Min(URows/2, 20); ++i)
			{

                if ((URows > 7) && (i != 7) && (i != 10) && (i != 11) && (i != 12) && (i != 13) && (i != 15) && (i != 20)) continue;
				double q = computeInitialAssumptionsForDocumentsForLSA(i, U, URows, UCols, 0.01f);
				Console.WriteLine("With {0} means - {1}", i, q);
				computeRecognitionRatioForPLSA(10);
			}
		}

		private static void FitInitialApproximation()
		{
            string path = dataFolder;
			string MatrixUt = Path.Combine(path, "result");
			//домножаем каждый документ на сингулярное значение
			if(U == null)
			    prepareDocDocMatrix2(MatrixUt);
            for (int i = 3; i < Math.Min(URows / 2, 20); ++i)
            {
                if ((URows > 7) && (i != 7) && (i != 10) && (i != 11) && (i != 12) && (i != 13) && (i != 15) && (i != 20)) continue;
                double q = computeInitialAssumptionsForDocumentsForLSA(i, U, URows, UCols, 0.1f);
                Console.WriteLine("With {0} means - {1}", i, q);
            }
		}

		private static double computeInitialAssumptionsForDocumentsForLSA(int k, float[,] M, int mRows, int mCols, float stopDelta)
		{
			bool stop = false;
			const int MAX_TIMES = 5;
			float[][] weights = new float[mRows][];
			float[][] weights2 = new float[mRows][];
			double[] qual = new double[MAX_TIMES];
			for (int i = 0; i < mRows; ++i )
			{
				weights[i] = new float[k];
				weights2[i] = new float[k];
			}
			float[][] centers = new float[k][];
			double delta;
			double bestQ = double.MaxValue;
			for (int i = 0; i < k; ++i)
				centers[i] = new float[mCols];
			for (int times = 0; times < MAX_TIMES; ++times)
			{
				PLSA.initializeMatrix(weights, mRows, k);
				PLSA.normalizeMatrix(weights, mRows, k);
				while (!stop)
				{
					//find new centers
					for (int i = 0; i < k; ++i)
						for (int j = 0; j < mCols; ++j)
							centers[i][j] = 0.0F;
					for (int i = 0; i < k; ++i)
					{
						for (int j = 0; j < mRows; ++j)
						{
							for (int z = 0; z < mCols; ++z)
								centers[i][z] += M[j,z] * weights[j][i];
						}
					}
					//good
					//now let's find new weights
					for (int i = 0; i < mRows; ++i)
					{
						for (int j = 0; j < k; ++j)
						{
							double coeff = 0.0;
							double norm1 = 0.0;
							double norm2 = 0.0;
							for (int z = 0; z < mCols; ++z)
							{
								coeff += M[i, z] * centers[j][z];
								norm1 += M[i, z] * M[i, z];
								norm2 += centers[j][z] * centers[j][z];
							}
							coeff /= Math.Sqrt(norm1);
							coeff /= Math.Sqrt(norm2);
							weights2[i][j] = weights[i][j];
							weights[i][j] = (float)coeff;
							if (weights[i][j] < 0)
								weights[i][j] = 0;
						}
					}
					PLSA.normalizeMatrix(weights, mRows, k);
					//good now let's find delta
					delta = 0.0; 
					for (int i = 0; i < mRows; ++i)
						for (int j = 0; j < k; ++j)
							delta += Math.Abs(weights2[i][j] - weights[i][j]);
					stop = delta < stopDelta;
				}
				//CMeans with given K is set up. Let's get it's quality!
				//inner is just weighted sum of squares
				double q = 0.0;
				for (int i = 0; i < mRows; ++i)
					for (int j = 0; j < k; ++j)
					{
						double coeff = 0.0;
						double norm1 = 0.0;
						double norm2 = 0.0;
						for (int z = 0; z < mCols; ++z)
						{
							coeff += M[i, z] * centers[j][z];
							norm1 += M[i, z] * M[i, z];
							norm2 += centers[j][z] * centers[j][z];
						}
						coeff /= Math.Sqrt(norm1);
						coeff /= Math.Sqrt(norm2);
						q += (1 - coeff) * weights[i][j];
					}
				qual[times] = q;
				if (q < bestQ)
				{
					bestQ = q;
					if (m_initialApproximation.GetLength(1) < k)
					{
						m_initialApproximation = (float[,])Array.CreateInstance(typeof(float), mRows, k);
					}
					for (int i = 0; i < mRows; ++i)
						for (int j = 0; j < k; ++j )
						{
							m_initialApproximation[i,j] = weights[i][j];
						}
				}
			}
			Array.Sort(qual);
			return qual[MAX_TIMES/2];//median
		}

		private static void computeTopTermsForLSA(int topLength)
		{
            string path = dataFolder;
			string MatrixUt = Path.Combine(path, "result");
			//домножаем каждый документ на сингулярное значение
			prepareTermTermMatrix(MatrixUt);
			string namePath = Path.Combine(path, "TermList.txt");
			List<string> fNames = new List<string>();
			using (StreamReader sr = new StreamReader(namePath))
			{
				while (sr.Peek() != -1)
				{
					fNames.Add(sr.ReadLine());
				}
			}
			string distDocFile = Path.Combine(path, "DistTermFile.txt");
			List<KeyValuePair<double, int>> top = new List<KeyValuePair<double, int>>();
			StreamWriter resWriter = new StreamWriter(Path.Combine(path, "simTerms.txt"));
			StreamWriter distWriter = new StreamWriter(distDocFile);
			// цикл по всем документам
			for (int i = 0; i < VRows; ++i)
			{
				top.Clear();
				for (int z = 0; z < VRows; ++z)
				{
					double coeff = 0.0;
					double norm1 = 0.0;
					double norm2 = 0.0;
					for (int j = 0; j < VCols; ++j)
					{
						coeff += V[i, j] * V[z, j];
						norm1 += V[i, j] * V[i, j];
						norm2 += V[z, j] * V[z, j];
					}
					coeff /= Math.Sqrt(norm1);
					coeff /= Math.Sqrt(norm2);
					if (i != z)
						top.Add(new KeyValuePair<double, int>(coeff, z));
					distWriter.Write("{0} ", coeff);
				}
				distWriter.WriteLine();
				top.Sort(new ReverseComparer());
				IEnumerator<KeyValuePair<double, int>> en = top.GetEnumerator();
				en.MoveNext();
				for (int k = 0; k < topLength; ++k)
				{
					resWriter.WriteLine(fNames[i] + "|" + fNames[en.Current.Value] + "|" + en.Current.Key);
					en.MoveNext();
				}
				resWriter.WriteLine();
			}
			resWriter.Close();
			distWriter.Close();
		}

		private static void computeTagsForLSA(int topLength)
		{
            string path = dataFolder;
			string MatrixUt = Path.Combine(path, "result");
			//домножаем каждый документ на сингулярное значение
			prepareDocDocMatrix2(MatrixUt);
			prepareTermTermMatrix(MatrixUt);
			string namePath = Path.Combine(path, "ProcessedFilesList.txt");
			List<string> fNames = new List<string>();
			using (StreamReader sr = new StreamReader(namePath))
			{
				while (sr.Peek() != -1)
				{
					fNames.Add(sr.ReadLine());
				}
			}
			namePath = Path.Combine(path, "TermList.txt");
			List<string> termNames = new List<string>();
			using (StreamReader sr = new StreamReader(namePath))
			{
				while (sr.Peek() != -1)
				{
					termNames.Add(sr.ReadLine());
				}
			}
			List<KeyValuePair<double, int>> top = new List<KeyValuePair<double, int>>();
			StreamWriter resWriter = new StreamWriter(Path.Combine(path, "Tags.txt"));
			// цикл по всем документам
			for (int i = 0; i < URows; ++i)
			{
				top.Clear();
				for (int z = 0; z < VRows; ++z)
				{
					if (i == z)
						continue;
					double coeff = 0.0;
					double norm1 = 0.0;
					double norm2 = 0.0;
					for (int j = 0; j < UCols; ++j)
					{
						coeff += U[i, j] * V[z, j];
						norm1 += U[i, j] * U[i, j];
						norm2 += V[z, j] * V[z, j];
					}
					coeff /= Math.Sqrt(norm1);
					coeff /= Math.Sqrt(norm2);
					top.Add(new KeyValuePair<double, int>(coeff, z));
				}
				top.Sort(new ReverseComparer());
				IEnumerator<KeyValuePair<double, int>> en = top.GetEnumerator();
				en.MoveNext();
				for (int k = 0; k < topLength; ++k)
				{
					resWriter.WriteLine(fNames[i] + "|" + termNames[en.Current.Value] + "|" + en.Current.Key);
					en.MoveNext();
				}
				resWriter.WriteLine();
			}
			resWriter.Close();
		}

		private static void computeRecognitionRatioForPLSA(int topLength)
		{
            string path = dataFolder;
			string dataFileName = Path.Combine(path, "DocWordMatrix.dat");
			FileInfo fi = new FileInfo(dataFileName);
			int nRecords = (int)fi.Length;
			nRecords /= 10;
			int maxRow = 0;
			int maxCol = 0;
			int nRows = 0;
			int nCols = 0;
			using (FileStream stream = new FileStream(dataFileName, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					for (int x = 0; x < nRecords; ++x)
					{
						nRows = reader.ReadInt32();
						nCols = reader.ReadInt32();
						short value = reader.ReadInt16();
						if (nRows > maxRow) maxRow = nRows;
						if (nCols > maxCol) maxCol = nCols;
					}
					reader.Close();
				}
				stream.Close();
			}
			++maxRow;
			++maxCol;
			nRows = maxRow;
			nCols = maxCol;
			data = new float[nRows][];
			for (int i = 0; i < nRows; ++i)
			{
				data[i] = new float[nCols];
			}
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					data[i][j] = 0.0f;
				}
			}
			using (FileStream stream = new FileStream(dataFileName, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					for (int x = 0; x < nRecords; ++x)
					{
						nRows = reader.ReadInt32();
						nCols = reader.ReadInt32();
						short value = reader.ReadInt16();
						data[nRows][nCols] = (float)(value);
					}
					reader.Close();
				}
				stream.Close();
			}
			//At this point we apply PLSA
			int nCategories = m_initialApproximation.GetLength(1);
			nRows = maxRow;
			nCols = maxCol;
			float[][] D1 = new float[nRows][];
			for (int i = 0; i < nRows; ++i)
			{
				D1[i] = new float[nCategories];
			}
			float[][] D2 = new float[nRows][];
			for (int i = 0; i < nRows; ++i)
			{
				D2[i] = new float[nCategories];
			}
			float[][] W1 = new float[nCategories][];
			for (int i = 0; i < nCategories; ++i)
			{
				W1[i] = new float[nCols];
			}
			float[][] W2 = new float[nCategories][];
			for (int i = 0; i < nCategories; ++i)
			{
				W2[i] = new float[nCols];
			}
			float[][] N = new float[nRows][];
			for (int i = 0; i < nRows; ++i)
			{
				N[i] = new float[nCols];
			}
			float[] Z = new float[nCategories];
			//Here we assign result of LSA as intial approximation
			Random rnd = new Random();
			double s;
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCategories; ++j)
                   	D1[i][j] = m_initialApproximation[Math.Min(i, m_initialApproximation.Length/nCategories -1) ,j];
			}
			NMF.makeN(data, N, nRows, nCols);
			float theoreticalLimitOfLikelihood = 0.0f;
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					if (data[i][j] > 0)
					{
						theoreticalLimitOfLikelihood += (float)(data[i][j] * Math.Log(N[i][j]));
					}
				}
			}
			Console.WriteLine("Start pLSA NMF, theoretical limit for likelihood {0:f}\n", theoreticalLimitOfLikelihood);
			StreamWriter likeWriter = new StreamWriter(Path.Combine(path, "Likelihood.txt"), true);
			#region OldIf
			if (bUsePLSA == false)
			{
			    NMF.initializeMatrix(W1, nCategories, nCols);
			    NMF.normalizeMatrix(D1, nRows, nCategories);
			    NMF.normalizeMatrix(W1, nCategories, nCols);
			    NMF.Flush(nRows, nCols, nCategories);
			    while (!NMF.makeApproximationStepNMF(data, D1, D2, W1, W2, N, nRows, nCols, nCategories, 1)) { }
			    for (int i = 0; i < nRows; ++i)
			    {
			        for (int j = 0; j < nCategories; ++j)
			        {
			            D2[i][j] = D1[i][j];
			        }
			    }
			    float likelihood = NMF.computeLikelihood(data, D2, W1, nRows, nCols, nCategories);
			    Console.WriteLine("The likelihood {0:f} \r", likelihood);
			    likelihood = PLSA.computeLikelihood(data, D2, W1, nRows, nCols, nCategories);
			    Console.WriteLine("The PLSA likelihood {0:f} \r", likelihood);
			    likeWriter.WriteLine("{0}|{1}", nCategories, likelihood);
			    likeWriter.Close();
			}
			else
			{
			    PLSA.initializeMatrix(W1, nCategories, nCols);
			    PLSA.normalizeMatrix(D1, nRows, nCategories);
			    PLSA.normalizeMatrix(W1, nCategories, nCols);
			    PLSA.initializeZ(Z, nCategories);
			    int nCounter = 0;
			    while (!PLSA.makeApproximationStep(D1, D2, W1, W2, N, Z, data, nRows, nCols, nCategories))
			    {
			        ++nCounter;
			        if (nCounter >= 300) break;
			    }
			    Console.WriteLine("\n");
			    PLSA.normalizeMatrix(D2, nRows, nCategories);
			    PLSA.normalizeMatrix(W2, nCategories, nCols);
			    float likelihood = NMF.computeLikelihood(data, D2, W1, nRows, nCols, nCategories);
			    Console.WriteLine("The likelihood {0:f} \r", likelihood);
			    likelihood = PLSA.computeLikelihood(data, D2, W1, nRows, nCols, nCategories);
			    Console.WriteLine("The PLSA likelihood {0:f} \r", likelihood);
			    likeWriter.WriteLine("{0}|{1}", nCategories, likelihood);
			    likeWriter.Close();
			}
			#endregion
			Console.WriteLine("Learning completed, outputing similarities");
			string namePath = Path.Combine(path, "ProcessedFilesList.txt");
			List<string> fNames = new List<string>();
			using (StreamReader sr = new StreamReader(namePath))
			{
				while (sr.Peek() != -1)
				{
					fNames.Add(sr.ReadLine());
				}
			}
			namePath = Path.Combine(path, "TermList.txt");
			List<string> termNames = new List<string>();
			using (StreamReader sr = new StreamReader(namePath))
			{
				while (sr.Peek() != -1)
				{
					termNames.Add(sr.ReadLine());
				}
			}
			List<KeyValuePair<double, int>> top = new List<KeyValuePair<double, int>>();
			List<KeyValuePair<double, int>> top2 = new List<KeyValuePair<double, int>>();
#region TopDocuments
			StreamWriter resWriter = new StreamWriter(Path.Combine(path, String.Format("simFiles_{0}.txt", nCategories)));
			StreamWriter distDocWriter = new StreamWriter(Path.Combine(path, String.Format("distDoc_{0}.txt", nCategories)));
			StreamWriter resWriter2 = new StreamWriter(Path.Combine(path, String.Format("simFiles2_{0}.txt", nCategories)));
			StreamWriter distDocWriter2 = new StreamWriter(Path.Combine(path, String.Format("distDoc2_{0}.txt", nCategories)));
			for (int i = 0; i < nRows; ++i)
			{
				top.Clear();
				top2.Clear();
				for (int j = 0; j < nRows; ++j)
				{
					s = 0.0;
					double coeff = 0.0;
					double norm1 = 0.0;
					double norm2 = 0.0;
					for (int z = 0; z < nCategories; ++z)
					{
						s += D2[i][z] * D2[j][z];
						coeff += D2[i][z] * D2[j][z];
						norm1 += D2[i][z] * D2[i][z];
						norm2 += D2[j][z] * D2[j][z];
					}
					coeff /= Math.Sqrt(norm1);
					coeff /= Math.Sqrt(norm2);
					if (i != j)
					{
						top.Add(new KeyValuePair<double, int>(s, j));
						top2.Add(new KeyValuePair<double, int>(coeff, j));
					}
					distDocWriter.Write("{0} ", s);
					distDocWriter2.Write("{0} ", coeff);
				}
				distDocWriter.WriteLine();
				distDocWriter2.WriteLine();
				top.Sort(new ReverseComparer());
				IEnumerator<KeyValuePair<double, int>> en = top.GetEnumerator();
				en.MoveNext();
				for (int k = 0; k < topLength; ++k)
				{
					resWriter.WriteLine(fNames[i] + "|" + fNames[en.Current.Value] + "|" + en.Current.Key);
					en.MoveNext();
				}
				resWriter.WriteLine();
				top2.Sort(new ReverseComparer());
				en = top2.GetEnumerator();
				en.MoveNext();
				for (int k = 0; k < topLength; ++k)
				{
					resWriter2.WriteLine(fNames[i] + "|" + fNames[en.Current.Value] + "|" + en.Current.Key);
					en.MoveNext();
				}
				resWriter2.WriteLine();
			}
			resWriter.Close();
			resWriter2.Close();
			distDocWriter.Close();
			distDocWriter2.Close();
#endregion
#region Top tags
			resWriter = new StreamWriter(Path.Combine(path, String.Format("Tags_{0}.txt", nCategories)));
			resWriter2 = new StreamWriter(Path.Combine(path, String.Format("Tags2_{0}.txt", nCategories)));
			namePath = Path.Combine(path, "TermList.txt");
			termNames = new List<string>();
			using (StreamReader sr = new StreamReader(namePath))
			{
				while (sr.Peek() != -1)
				{
					termNames.Add(sr.ReadLine());
				}
			}
			List<KeyValuePair<double, int>> top3 = new List<KeyValuePair<double, int>>();
			List<KeyValuePair<double, int>> top4 = new List<KeyValuePair<double, int>>();
			for (int i = 0; i < nRows; ++i)
			{
				top.Clear();
				top2.Clear();
				top3.Clear();
				top4.Clear();
				for (int j = 0; j < nCols; ++j)
				{
					s = 0.0;
					double coeff = 0.0;
					double norm1 = 0.0;
					double norm2 = 0.0;
					for (int z = 0; z < nCategories; ++z)
					{
						s += D2[i][z] * W1[z][j];
						coeff += D2[i][z] * W1[z][j];
						norm1 += D2[i][z] * D2[i][z];
						norm2 += W1[z][j] * W1[z][j];
					}
					coeff /= Math.Sqrt(norm1);
					coeff /= Math.Sqrt(norm2);
					top.Add(new KeyValuePair<double, int>(s, j));
					top2.Add(new KeyValuePair<double, int>(coeff, j));
					if (data[i][j] > 0.0f)
					{
						top3.Add(new KeyValuePair<double, int>(s, j));
						top4.Add(new KeyValuePair<double, int>(coeff, j));
					}
				}
				top3.Sort(new ReverseComparer());
				IEnumerator<KeyValuePair<double, int>> _enum = top3.GetEnumerator();
                _enum.MoveNext();
                resWriter.WriteLine("Inner:");
				for (int k = 0; k < topLength; ++k)
				{
					resWriter.WriteLine(fNames[i] + "|" + termNames[_enum.Current.Value] + "|" + _enum.Current.Key);
					_enum.MoveNext();
				}
				resWriter.WriteLine("Out:");
				top.Sort(new ReverseComparer());
				_enum = top.GetEnumerator();
				_enum.MoveNext();
				for (int k = 0; k < topLength; ++k)
				{
					resWriter.WriteLine(fNames[i] + "|" + termNames[_enum.Current.Value] + "|" + _enum.Current.Key);
					_enum.MoveNext();
				}
				resWriter.WriteLine();
				top4.Sort(new ReverseComparer());
				_enum = top4.GetEnumerator();
				_enum.MoveNext();
				for (int k = 0; k < topLength; ++k)
				{
					resWriter2.WriteLine(fNames[i] + "|" + termNames[_enum.Current.Value] + "|" + _enum.Current.Key);
					_enum.MoveNext();
				}
				resWriter2.WriteLine("Out:");
				top2.Sort(new ReverseComparer());
				_enum = top2.GetEnumerator();
				_enum.MoveNext();
				for (int k = 0; k < topLength; ++k)
				{
					resWriter2.WriteLine(fNames[i] + "|" + termNames[_enum.Current.Value] + "|" + _enum.Current.Key);
					_enum.MoveNext();
				}
				resWriter2.WriteLine();
			}
			resWriter.Close();
			resWriter2.Close();
#endregion
		}

		private static void ConvertMatrixFromCSV(string csvName, string newName)
		{
			using (StreamReader sr = new StreamReader(csvName))
			{
				using (BinaryWriter br = new BinaryWriter(new FileStream(newName, FileMode.OpenOrCreate)))
				{
					char[] seps = { ',', ' ' };
					while(sr.Peek() != -1)
					{
						string s = sr.ReadLine();
						string[] parts = s.Split(seps);
						int docID = Convert.ToInt32(parts[0]);
						int termID = Convert.ToInt32(parts[2]);
						short freq = Convert.ToInt16(parts[4]);
						br.Write(docID);
						br.Write(termID);
						br.Write(freq);
					}
					br.Flush();
					br.Close();
				}
				sr.Close();
			}
		}

		private static void ConvertMatrixToCSV(string binName, string csvName)
		{
			using (BinaryReader br = new BinaryReader(new FileStream(binName, FileMode.Open)))
			{
				FileInfo fi = new FileInfo(binName);
				long nBytes = fi.Length;
				int nNonZeros = (int)(nBytes / 10);
				using (StreamWriter tr = new StreamWriter(new FileStream(csvName, FileMode.OpenOrCreate)))
				{
					for (int i = 0; i < nNonZeros; ++i )
					{
						int docID = br.ReadInt32();
						int termID = br.ReadInt32();
						short freq = br.ReadInt16();
						tr.WriteLine("{0}, {1}, {2}", docID, termID, freq);
					}
					tr.Flush();
					tr.Close();
				}
				br.Close();
			}
		}

		// In case of usage from within VS.
		static void Main(string[] args)
		{
            // Arguments: 
            // args[0..N] - names of folders with documents (that contain not originals, but "*.parsed.win"!)
            // args[N+1...M] - names of individual files, not belonging to 
            // any folder (not originals, but "*.parsed.win"!)
			DateTime start = DateTime.Now;
            //we set root folders for user's documentsa and paths of individual files
            int i = 0;
            string[] rootFolders = new string[args.Length];
            while (i < args.Length)
            {
                if (System.IO.Directory.Exists(args[i]) || System.IO.File.Exists(args[i]))
                 rootFolders[i] = args[i];
                i++;
            }
			string extension = "*.parsed.win";
            string path = dataFolder;
			string matrixFile = Path.Combine(path, "matrix");
			string resultFile = Path.Combine(path, "result");
			List<string> files = new List<string>();
			files.Clear();
			enumerateFiles(files, rootFolders, extension);
			m_initialApproximation = (float[,])Array.CreateInstance(typeof(float), files.Count(), nCategories);
			processFiles(ref files);
			ConvertMatrixToCSV(Path.Combine(path, "DocWordMatrix.dat"), Path.Combine("flashMatrix.csv"));
			reformatMatrix();
			SVD.ProcessData(matrixFile, resultFile, true);
			//At this point the data SVD completed and next part is
			//simple test for particular technology PLSA and for specific 
			//data which is PATENTCORPUS128.  For a different data
			//it has to be adjusted.
			FitPLSAWithInitialApproximation();
			DateTime end = DateTime.Now;
			TimeSpan duration = end - start;
			double time = duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
			Console.WriteLine("Total processing time {0:########.00} seconds", time);
		}
	}
}
