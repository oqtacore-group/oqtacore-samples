// pLSADemo.cpp : Defines the entry point for the console application.
// Quick demo of Probabilistic Latent Semantic Analysis
// and Non-negative Matrix Factorization, written by Andrew Polar.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pLSAtest
{
    static class NMF
    {
        static float prevLikelihood = 0.0f;
		static public float bestLikelihood = float.MinValue;
		static float[][] bestW;
		static float[][] bestD;

		public static void Flush(int nRows, int nCols, int nCats)
		{
			bestLikelihood = float.MinValue;
			bestW = new float[nCats][];
			for (int i = 0; i < nCats; ++i)
				bestW[i] = new float[nCols];

			bestD = new float[nRows][];
			for (int i = 0; i < nRows; ++i)
				bestD[i] = new float[nCats];
		}

        //auxuliar functions
        public static void printVector(string title, float[] Vector, int n)
        {
            Console.WriteLine(title);
            for (int i = 0; i < n; ++i)
            {
                Console.Write(" {0:f} ", Vector[i]);
            }
            Console.WriteLine("\n");
        }

        public static void printMatrix(string title, float[][] Matrix, int nRows, int nCols)
        {
            Console.WriteLine(title);
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    Console.Write(" {0:f} ", Matrix[i][j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("\n");
        }

        public static void normalizeMatrix(float[][] Matrix, int nRows, int nCols)
        {
            for (int i = 0; i < nRows; ++i)
            {
                float min = Matrix[i][0];
                for (int j = 0; j < nCols; ++j)
                {
                    if (min > Matrix[i][j]) min = Matrix[i][j];
                }
                if (min < 0.0)
                {
                    for (int j = 0; j < nCols; ++j)
                    {
                        Matrix[i][j] -= min;
                    }
                }
                float s = 0.0f;
                for (int j = 0; j < nCols; ++j)
                {
                    s += Matrix[i][j];
                }
                if (s > 0.0)
                {
                    for (int j = 0; j < nCols; ++j)
                    {
                        Matrix[i][j] /= s;
                    }
                }
            }
        }

        public static void initializeMatrix(float[][] Matrix, int nRows, int nCols)
        {
            Random rand = new Random();
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    Matrix[i][j] = (float)(rand.Next() % 100);
                    Matrix[i][j] /= 100.0f;
                }
            }
        }

        static void initializeZ(float[] Z, int nSize)
        {
            for (int i = 0; i < nSize; ++i) Z[i] = 1.0f / (float)(nSize);
        }

        public static float computeLikelihood(float[][] data, float[][] D, float[][] W, int nRows, int nCols, int nCats)
        {
            float likelihood = 0.0f;
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    if (data[i][j] > 0.0)
                    {
                        float s = 0.0f;
                        for (int k = 0; k < nCats; ++k)
                        {
                            s += D[i][k] * W[k][j];
                        }
                        if (s > 0.0)
                        {
                            likelihood += data[i][j] * (float)(Math.Log(s));
                        }
                    }
                }
            }
            return likelihood;
        }
        //end

        //Cholesky decomposition
        static void choldc1(int n, float[][] a, float[] p)
        {
            int i, j, k;
            float sum;
            for (i = 0; i < n; i++)
            {
                for (j = i; j < n; j++)
                {
                    sum = a[i][j];
                    for (k = i - 1; k >= 0; k--)
                    {
                        sum -= a[i][k] * a[j][k];
                    }
                    if (i == j)
                    {
                        if (sum <= 0)
                        {
                            p[i] = 1.0f; //regularization
                        }
                        else
                        {
                            p[i] = (float)(Math.Sqrt(sum));
                        }
                    }
                    else
                    {
                        a[j][i] = sum / p[i];
                    }
                }
            }
        }

        static void choldc(int n, float[][] A, float[][] a)
        {
            int i, j;
            float[] p = new float[n];
            for (i = 0; i < n; i++)
            {
                for (j = 0; j < n; j++)
                {
                    a[i][j] = A[i][j];
                }
            }
            choldc1(n, a, p);
            for (i = 0; i < n; i++)
            {
                a[i][i] = p[i];
                for (j = i + 1; j < n; j++)
                {
                    a[i][j] = 0;
                }
            }
        }

        //inverts positive definite M, returns result in M 
        static void choleskyInvert(float[][] M, int size)
        {
            float[][] T = new float[size][];
            for (int i = 0; i < size; ++i)
            {
                T[i] = new float[size];
            }
            choldc(size, M, T); //find lower triangular
            //make it symmetric
            for (int i = 0; i < size; ++i)
            {
                for (int j = i + 1; j < size; ++j)
                {
                    T[i][j] = T[j][i];
                }
            }
            float[] v = new float[size];
            float[] e = new float[size];
            for (int loop = 0; loop < size; ++loop)
            {
                for (int i = 0; i < size; ++i) e[i] = 0.0f;
                e[loop] = 1.0f;
                //solve lower triangular
                for (int i = 0; i < size; ++i)
                {
                    float sum = 0.0f;
                    for (int j = 0; j < i; ++j)
                    {
                        sum += T[i][j] * v[j];
                    }
                    v[i] = (e[i] - sum) / T[i][i];
                }
                //solve upper triangular
                for (int i = size - 1; i >= 0; --i)
                {
                    float sum = 0.0f;
                    for (int j = size - 1; j > i; --j)
                    {
                        sum += T[i][j] * M[j][loop];
                    }
                    M[i][loop] = (v[i] - sum) / T[i][i];
                }
            }
        }
        //end Cholesky inversion

        //non-negative matrix factorization
        public static void makeN(float[][] data, float[][] N, int nRows, int nCols)
        {
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    N[i][j] = data[i][j];
                }
            }
            normalizeMatrix(N, nRows, nCols);
        }

        static void getNewW(float[][] data, float[][] D, float[][] W, float[][] N, int nRows, int nCols, int nCats)
        {
            //it is D = N * WT multiplication
            for (int nWhich = 0; nWhich < nCats; ++nWhich)
            {
                for (int i = 0; i < nRows; ++i)
                {
                    D[i][nWhich] = 0.0f;
                    for (int j = 0; j < nCols; ++j)
                    {
                        if (data[i][j] > 0.0)
                        {
                            D[i][nWhich] += N[i][j] * W[nWhich][j];
                        }
                    }
                }
            }
            //copy D into N
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCats; ++j)
                {
                    N[i][j] = D[i][j];
                }
            }
            //it is WWT computation
            float[][] WWT = new float[nCats][];
            for (int i = 0; i < nCats; ++i)
            {
                WWT[i] = new float[nCats];
            }
            for (int i = 0; i < nCats; ++i)
            {
                for (int j = 0; j < nCats; ++j)
                {
                    WWT[i][j] = 0.0f;
                    for (int k = 0; k < nCols; ++k)
                    {
                        WWT[i][j] += W[i][k] * W[j][k];
                    }
                }
            }
            choleskyInvert(WWT, nCats);
            //it is inverse(WWT) * D multiplication
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCats; ++j)
                {
                    D[i][j] = 0.0f;
                    for (int k = 0; k < nCats; ++k)
                    {
                        D[i][j] += N[i][k] * WWT[k][j];
                    }
                }
            }
            normalizeMatrix(D, nRows, nCats);
        }

        static void getNewD(float[][] data, float[][] D, float[][] W, float[][] N, int nRows, int nCols, int nCats)
        {
            //it is W = DT * N multiplication
            for (int nWhich = 0; nWhich < nCats; ++nWhich)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    W[nWhich][j] = 0.0f;
                    for (int i = 0; i < nRows; ++i)
                    {
                        if (N[i][j] > 0.0)
                        {
                            W[nWhich][j] += N[i][j] * D[i][nWhich];
                        }
                    }
                }
            }
            //copy W into N
            for (int i = 0; i < nCats; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    N[i][j] = W[i][j];
                }
            }
            //it is DTD computation
            float[][] DTD = new float[nCats][];
            for (int i = 0; i < nCats; ++i)
            {
                DTD[i] = new float[nCats];
            }
            for (int i = 0; i < nCats; ++i)
            {
                for (int j = 0; j < nCats; ++j)
                {
                    DTD[i][j] = 0.0f;
                    for (int k = 0; k < nRows; ++k)
                    {
                        DTD[i][j] += D[k][i] * D[k][j];
                    }
                }
            }
            choleskyInvert(DTD, nCats);
            //it is inverse(DTD) * W multiplication
            for (int i = 0; i < nCols; ++i)
            {
                for (int j = 0; j < nCats; ++j)
                {
                    W[j][i] = 0.0f;
                    for (int k = 0; k < nCats; ++k)
                    {
                        W[j][i] += DTD[k][j] * N[k][i];
                    }
                }
            }
            normalizeMatrix(W, nCats, nCols);
        }

		public static void CopyMatrices(float[][] dst, float[][] src, int nRows, int nCols)
		{
			for (int i = 0; i < nRows; ++i)
				for (int j = 0; j < nCols; ++j)
					dst[i][j] = src[i][j];
		}

        public static bool makeApproximationStepNMF(float[][] data, float[][] D1, float[][] D2, float[][] W1, float[][] W2, float[][] N, int nRows, int nCols, int nCats, int step)
        {
			CopyMatrices(D2, D1, nRows, nCats);
			CopyMatrices(W2, W1, nCats, nCols);
            makeN(data, N, nRows, nCols);
            getNewD(data, D1, W1, N, nRows, nCols, nCats);
            makeN(data, N, nRows, nCols);
            getNewW(data, D1, W1, N, nRows, nCols, nCats);
            float likelihood = computeLikelihood(data, D1, W1, nRows, nCols, nCats);
            Console.WriteLine("current likelihood {0:f} \r", likelihood);
			if (likelihood > bestLikelihood)
			{
				bestLikelihood = likelihood;
				CopyMatrices(bestD, D1, nRows, nCats);
				CopyMatrices(bestW, W1, nCats, nCols);
			}
			float difference = 0.0f;
			for (int i = 0; i < nRows; ++i)
			{
				for (int j = 0; j < nCats; ++j)
				{
					difference += Math.Abs(D1[i][j] - D2[i][j]);
					D2[i][j] = D1[i][j];
				}
			}
			for (int i = 0; i < nCats; ++i)
			{
				for (int j = 0; j < nCols; ++j)
				{
					difference += Math.Abs(W1[i][j] - W2[i][j]);
					W2[i][j] = W1[i][j];
				}
			}
			difference /= (float)(nCats);
			difference /= (float)(nCats);
			if (difference < 0.1 || step > 10)
			{
				//setting the best case
				CopyMatrices(D1, bestD, nRows, nCats);
				CopyMatrices(W1, bestW, nCats, nCols);
				return true;
			}
            prevLikelihood = likelihood;
            return false;
        }
    }

    static class PLSA
    {
        public static void printMatrix(float[][] Matrix, int nRows, int nCols)
        {
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    Console.Write(" {0:f} ", Matrix[i][j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public static void printVector(float[] x, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                Console.Write(" {0:f} ", x[i]);
            }
        }

        public static void normalizeMatrix(float[][] Matrix, int nRows, int nCols)
        {
            for (int i = 0; i < nRows; ++i)
            {
                float s = 0.0f;
                for (int j = 0; j < nCols; ++j)
                {
                    s += Matrix[i][j];
                }
                for (int j = 0; j < nCols; ++j)
                {
                    if (s > 0.0) Matrix[i][j] /= s;
                }
            }
        }

        public static float computeLikelihood(float[][] data, float[][] D, float[][] W, int nRows, int nCols, int nCats)
        {
            float likelihood = 0.0f;
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    if (data[i][j] > 0.0)
                    {
                        float s = 0.0f;
                        for (int k = 0; k < nCats; ++k)
                        {
                            s += D[i][k] * W[k][j];
                        }
                        if (s > 0.0)
                        {
                            likelihood += data[i][j] * (float)(Math.Log(s));
                        }
                    }
                }
            }
            return likelihood;
        }

        public static void initializeMatrix(float[][] Matrix, int nRows, int nCols)
        {
            Random r = new Random();
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    Matrix[i][j] = (float)(r.Next(100));
                    Matrix[i][j] /= 100.0f;
                }
            }
        }

        public static void initializeZ(float[] Z, int nSize)
        {
            for (int i = 0; i < nSize; ++i) Z[i] = (float)(1.0) / (float)(nSize);
        }

        private static void getNewN(float[][] N, float[][] D, float[][] W, float[] Z, float[][] data, int nRows, int nCols, int nCategories)
        {
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    if (data[i][j] > 0.0)
                    {
                        float s = 0.0f;
                        for (int k = 0; k < nCategories; ++k)
                        {
                            //if you want slightly simplified form of PLSA remove Z[k], on my observation it may improve
                            //accuracy on a couple percent.
                            s += D[i][k] * W[k][j] * Z[k];
                        }
                        if (s > 0.0)
                        {
                            N[i][j] = data[i][j] / s;
                        }
                        else
                        {
                            N[i][j] = 0.0f;
                        }
                    }
                    else
                    {
                        N[i][j] = 0.0f;
                    }
                }
            }
        }

        private static void getNewD(float[][] D2, float[][] D1, float[][] W2, float[][] W1, float[][] N, float[] Z, float[][] data, int nRows, int nCols, int nCategories)
        {
            //it is D = N * WT multiplication
            for (int nWhich = 0; nWhich < nCategories; ++nWhich)
            {
                for (int i = 0; i < nRows; ++i)
                {
                    D2[i][nWhich] = 0.0f;
                    for (int j = 0; j < nCols; ++j)
                    {
                        if (data[i][j] > 0.0)
                        {
                            D2[i][nWhich] += N[i][j] * W1[nWhich][j];
                        }
                    }
                }
            }
            //it is Hadamard product
            for (int i = 0; i < nRows; ++i)
            {
                for (int nWhich = 0; nWhich < nCategories; ++nWhich)
                {
                    D2[i][nWhich] *= D1[i][nWhich];
                }
            }
        }

        private static void getNewW(float[][] W2, float[][] W1, float[][] D2, float[][] D1, float[][] N, float[] Z, float[][] data, int nRows, int nCols, int nCategories)
        {
            //it is W = DT * N multiplication
            for (int nWhich = 0; nWhich < nCategories; ++nWhich)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    W2[nWhich][j] = 0.0f;
                    for (int i = 0; i < nRows; ++i)
                    {
                        if (data[i][j] > 0.0)
                        {
                            W2[nWhich][j] += N[i][j] * D1[i][nWhich];
                        }
                    }
                }
            }
            //it is Hadamard product
            for (int nWhich = 0; nWhich < nCategories; ++nWhich)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    W2[nWhich][j] *= W1[nWhich][j];
                }
            }
        }

        public static void getNewZ(float[] Z, float[][] D, float[][] data, int nRows, int nCols, int nCats)
        {
            for (int nWhich = 0; nWhich < nCats; ++nWhich)
            {
                Z[nWhich] = 0.0f;
                for (int j = 0; j < nCols; ++j)
                {
                    for (int i = 0; i < nRows; ++i)
                    {
                        if (data[i][j] > 0.0)
                        {
                            Z[nWhich] += data[i][j] * D[i][nWhich];
                        }
                    }
                }
            }
            float s = 0.0f;
            for (int i = 0; i < nCats; ++i)
            {
                s += Z[i];
            }
            for (int i = 0; i < nCats; ++i)
            {
                Z[i] /= s;
            }
        }

        public static bool makeApproximationStep(float[][] D1, float[][] D2, float[][] W1, float[][] W2, float[][] N, float[] Z, float[][] data, int nRows, int nCols, int nCategories)
        {
            //This is E-step
            getNewN(N, D1, W1, Z, data, nRows, nCols, nCategories); 
            //Next is M-step
            getNewD(D2, D1, W2, W1, N, Z, data, nRows, nCols, nCategories);
            getNewW(W2, W1, D2, D1, N, Z, data, nRows, nCols, nCategories);
            getNewZ(Z, D1, data, nRows, nCols, nCategories);
            normalizeMatrix(D2, nRows, nCategories);
            normalizeMatrix(W2, nCategories, nCols);
            float likelihood = computeLikelihood(data, D2, W2, nRows, nCols, nCategories);
            Console.WriteLine("current likelihood {0:f} \r", likelihood);
            float difference = 0.0f;
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCategories; ++j)
                {
                    difference += Math.Abs(D1[i][j] - D2[i][j]);
                    D1[i][j] = D2[i][j];
                }
            }
            for (int i = 0; i < nCategories; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    difference += Math.Abs(W1[i][j] - W2[i][j]);
                    W1[i][j] = W2[i][j];
                }
            }
            difference /= (float)(nCategories);
            difference /= (float)(nCategories);
            if (difference < 0.1) return true;
            else
            {
                return false;
            }
        }
    }
}
