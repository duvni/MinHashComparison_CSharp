using System;
using System.Collections.Generic;
using System.Text;

namespace MinHashComparison
{
	public class MinHashSimilarity
	{
		/// <summary>
		/// The internal min hash instance
		/// </summary>
		private readonly MinHash _minHash;

		/// <summary>
		/// The threshold in which documents are considered similar
		/// </summary>
		private readonly double _threshold;

		/// <summary>
		/// Number of bands for LSH comparison
		/// </summary>
		private readonly int _bands;

		/// <summary>
		/// number of rows for LSH comparison
		/// </summary>
		private readonly int _rows;

		/// <summary>
		/// Buckets for LSH comparison
		/// </summary>
		private readonly Dictionary<string, List<int[]>> _buckets;

		/// <summary>
		/// Default Constructor
		/// Works best if threshold is ~90%
		/// </summary>
		/// <param name="threshold">The threshold in which documents are considered similar</param>
		public MinHashSimilarity(double threshold) : this (threshold, 5, 400, 20, 20)
		{
			// Likelihood of an LSH match between two documents (1-(1-J(A,B)^rows)^bands) | J(A,B) = Jaccard index, rows = 20, bands = 20
			// J(A,B)   Probability of getting compared
			// .7       .016
			// .8       .206
			// .85      .546
			// .861     .642 // sCurve ((1/b)^(1/r))
			// .87      .720
			// .9       .925
			// .95      .999
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="threshold">The threashold in which documents are considered similar</param>
		/// <param name="tokensInWord">number of tokens for a word</param>
		/// <param name="numHashFunctions">number of min hash functions to compute</param>
		/// <param name="bands">number of bands for LSH comparison</param>
		/// <param name="rows">number of rows for LSH comparison</param>
		public MinHashSimilarity(double threshold, int tokensInWord, int numHashFunctions, int bands, int rows)
		{
			if (threshold < 0 || threshold > 100)
			{
				throw new Exception(String.Format("MinHashSimilarity - Illegal threshold: {0}", threshold));
			}
			if (bands*rows != numHashFunctions)
			{
				throw new Exception("MinHashSimilarity - bands * rows != numHashFunctions");
			}

			_threshold = threshold;
			_minHash = new MinHash(tokensInWord, numHashFunctions);
			_bands = bands;
			_rows = rows;
			_buckets = new Dictionary<string, List<int[]>>();
		}

		/// <summary>
		/// Clears all history of documents
		/// </summary>
		public void ClearDocuments()
		{
			_buckets.Clear();
		}

		/// <summary>
		/// Given a string document, looks whether a similar document was already seen
		/// </summary>
		/// <param name="doc">The new document to compare to</param>
		/// <returns>true if a similar document was already seen</returns>
		public bool LookForSimilarDocument(string doc)
		{
			int[] minHashes = _minHash.ComputeSketch(doc.Split(new char[]{' ', '\t', '\r', '\n'}));
			string[] bandHashes = new string[_bands];
			HashSet<int[]> comparedSketches = new HashSet<int[]>();

			for (int i = 0; i < _bands; i++)
			{
				bandHashes[i] = ComputeBandHash(minHashes, i);

				if (_buckets.ContainsKey(bandHashes[i]))
				{
					foreach (int[] sketchToCompare in _buckets[bandHashes[i]])
					{
						if (!comparedSketches.Contains(sketchToCompare))
						{
							if (_minHash.CompareSketches(minHashes, sketchToCompare) >= _threshold)
							{
								// Found a similar document
								return true;
							}

							// Avoid comparing two documents twice
							comparedSketches.Add(sketchToCompare);
						}
					}
				}
			}

			// No match found, add document to buckets
			for (int i = 0; i < _bands; i++)
			{
				if (!_buckets.ContainsKey(bandHashes[i]))
				{
					_buckets.Add(bandHashes[i], new List<int[]>());
				}
				_buckets[bandHashes[i]].Add(minHashes);
			}

			return false;
		}

		/// <summary>
		/// Computes a hash for quick bucket match search
		/// </summary>
		/// <param name="minHashes">The MinHashes for row values</param>
		/// <param name="i">The ith band</param>
		/// <returns></returns>
		private string ComputeBandHash(int[] minHashes, int i)
		{
			StringBuilder bandHashSB = new StringBuilder((_rows + 1) * 10);
			for (int j = 0; j < _rows; j++)
			{
				// adding the rows corresponding to ith band
				bandHashSB.Append(minHashes[i * _rows + j].ToString().PadLeft(10, '0'));
			}
			// adding the number i to distinguish between bands
			bandHashSB.Append(i.ToString().PadLeft(10, '0'));
			return bandHashSB.ToString();
		}
	}
}
