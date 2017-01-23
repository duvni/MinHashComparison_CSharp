using System;
using System.Text;

namespace MinHashComparison
{
	public class MinHash
	{
		/// <summary>
		/// Number of hash functions
		/// </summary>
		private readonly int _numHashFunctions;

		/// <summary>
		/// Number of tokens in a word (k-shingling)
		/// </summary>
		private readonly int _tokensInWord;

		/// <summary>
		/// Array of hash functions
		/// </summary>
		private readonly HashGenerator[] _hashFunctions;

		/// <summary>
		/// Hash function generator.
		/// Given a set of hash function parameters (a, b, c) and a bound on possible hash value,
		/// generates a hash function that given an element x returns its hashed value.
		/// </summary>
		private class HashGenerator
		{
			private readonly int _a;
			private readonly int _b;
			private readonly int _c;
			private readonly int _universeSize;

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="a">Hash function parameter</param>
			/// <param name="b">Hash function parameter</param>
			/// <param name="c">Hash function parameter</param>
			/// <param name="universeSize">Upper bound on hash values - should be a Mersenne prime (e.g. 131071 = 2^17 - 1)</param>
			public HashGenerator(int a, int b, int c, int universeSize)
			{
				_a = a;
				_b = b;
				_c = c;
				_universeSize = universeSize;
			}

			/// <summary>
			/// Hash function calculator
			/// </summary>
			/// <param name="x">Hash function parameter</param>
			/// <returns>Hashed value</returns>
			public int CalculateHash(int x)
			{
				// Modify the hash family as per the size of possible elements in a Set
				x = x & _universeSize;
				int hashValue = (int)((_a * (x >> 4) + _b * x + _c) & _universeSize);
				return Math.Abs(hashValue);
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tokensInWord">number of tokens for a word</param>
		/// <param name="numHashFunctions">number of min hash functions to compute</param>
		public MinHash(int tokensInWord, int numHashFunctions)
		{
			if (tokensInWord <= 0)
			{
				throw new Exception(String.Format("MinHash - Illegal number of tokens in a word: {0}", tokensInWord));
			}

			if (numHashFunctions <= 0)
			{
				throw new Exception(String.Format("MinHash - Illegal number of hash functions: {0}", numHashFunctions));
			}

			_tokensInWord = tokensInWord;
			_numHashFunctions = numHashFunctions;

			// Generate all hash functions
			_hashFunctions = new HashGenerator[numHashFunctions];

			int universeSize = Int32.MaxValue; // Max Integer (2^31-1) is a Mersenne prime
			Random r = new Random();
			for (int i = 0; i < numHashFunctions; i++)
			{
				int a = r.Next(universeSize);
				int b = r.Next(universeSize);
				int c = r.Next(universeSize);
				_hashFunctions[i] = new HashGenerator(a, b, c, universeSize);
			}
		}

		/// <summary>
		/// Compute the MinHash Sketch from an array of tokens.
		/// Update the hash tables according to the min values of the sketch.
		/// </summary>
		/// <param name="tokens">A list of tokens</param>
		/// <returns>An array of minimun hash values</returns>
		public int[] ComputeSketch(string[] tokens)
		{
			// Maintain an array of minimum hash values
			int[] hashMinimumValues = new int[_numHashFunctions];

			// Since we're looking for minimum values,
			// it's important to initialize the array to max int
			for (int i = 0; i < hashMinimumValues.Length; i++)
			{
				hashMinimumValues[i] = Int32.MaxValue;
			}

			if (tokens == null || tokens.Length == 0)
			{
				return hashMinimumValues;
			}

			// Go over all tokens and generate words (k-shingling)
			for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
			{
				// Build the word by concatenating consecutive tokens
				StringBuilder wordBuilder = new StringBuilder(_tokensInWord);
				for (int i = tokenIndex; i < tokenIndex + _tokensInWord; i++)
				{
					if (i < tokens.Length)
					{
						wordBuilder.Append(tokens[i]);
					}
					else
					{
						break;
					}
				}

				int hashCode = wordBuilder.ToString().GetHashCode();

				// Go over all hash functions			
				for (int hashIndex = 0; hashIndex < _numHashFunctions; hashIndex++)
				{
					// compute hash value of token with current hash function
					int hashValue = _hashFunctions[hashIndex].CalculateHash(hashCode);

					// Update minimum value at index hashIndex
					hashMinimumValues[hashIndex] = Math.Min(hashMinimumValues[hashIndex], hashValue);
				}
			}

			// Return the MinHash Sketch
			return hashMinimumValues;
		}

		/// <summary>
		/// Compares two MinHash sketches
		/// </summary>
		/// <param name="firstMinHashSketch">The first MinHash sketch to compare</param>
		/// <param name="secondMinHashSketch">The second MinHash sketch to compare</param>
		/// <returns>Similarity result (between 0 and 1)</returns>
		public double CompareSketches(int[] firstMinHashSketch, int[] secondMinHashSketch)
		{
			// count equal hashes
			int equalHashes = 0;
			for (int i = 0; i < _numHashFunctions; i++)
			{
				if (firstMinHashSketch[i] == secondMinHashSketch[i])
				{
					equalHashes++;
				}
			}

			return (1.0 * equalHashes) / _numHashFunctions; // similarity index
		}
	}
}
