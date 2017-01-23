import java.util.Random;

public class MinHash {
	
	// Number of hash functions
	private int numHashFunctions;

	// Number of tokens in a word (k-shingling)
	private int tokensInWord;

	// Array of hash functions
	private HashGenerator[] hashFunctions;
	
	/**
	 * Hash function generator.
	 * Given a set of hash function parameters (a, b, c) and a bound on possible hash value,
	 * generates a hash function that given an element x returns its hashed value.
	 */
	private class HashGenerator
	{
		private int a;
		private int b;
		private int c;
		private int universeSize;

		
		/**
		 * Constructor
	 	 * @param a Hash function parameter
		 * @param b Hash function parameter
		 * @param c Hash function parameter
		 * @param universeSize Upper bound on hash values - should be a Mersenne prime (e.g. 131071 = 2^17 - 1)
		 */
		public HashGenerator(int a, int b, int c, int universeSize)
		{
			this.a = a;
			this.b = b;
			this.c = c;
			this.universeSize = universeSize;
		}

		/**
		 * Hash function calculator
		 * @param x Hash function parameter
		 * @return Hashed value
		 */
		public int calculateHash(int x)
		{
			// Modify the hash family as per the size of possible elements in a Set
			x = x & universeSize;
			int hashValue = (int)((a * (x >> 4) + b * x + c) & universeSize);
			return Math.abs(hashValue);
		}
	}
	
	/**
	 * Constructor
	 * @param tokensInWord Number of tokens for a word
	 * @param numHashFunctions Number of min hash functions to compute
	 * @throws Exception if an illegal value is used for one of the input parameters
	 */
	public MinHash(int tokensInWord, int numHashFunctions) throws Exception
	{
		if (tokensInWord <= 0)
		{
			throw new Exception(String.format("MinHash - Illegal number of tokens in a word: %d", tokensInWord));
		}

		if (numHashFunctions <= 0)
		{
			throw new Exception(String.format("MinHash - Illegal number of hash functions: %d", numHashFunctions));
		}

		this.tokensInWord = tokensInWord;
		this.numHashFunctions = numHashFunctions;

		// Generate all hash functions
		hashFunctions = new HashGenerator[numHashFunctions];

		int universeSize = Integer.MAX_VALUE; // Max Integer (2^31-1) is a Mersenne prime
		Random r = new Random();
		for (int i = 0; i < numHashFunctions; i++)
		{
			int a = r.nextInt(universeSize);
			int b = r.nextInt(universeSize);
			int c = r.nextInt(universeSize);
			hashFunctions[i] = new HashGenerator(a, b, c, universeSize);
		}
	}

	/**
	 * Compute the MinHash Sketch from an array of tokens.
	 * Update the hash tables according to the min values of the sketch.
	 * @param tokens A list of tokens
	 * @return An array of minimum hash values
	 */
	public int[] computeSketch(String[] tokens)
	{
		// Maintain an array of minimum hash values
		int[] hashMinimumValues = new int[numHashFunctions];

		// Since we're looking for minimum values,
		// it's important to initialize the array to max int
		for (int i = 0; i < hashMinimumValues.length; i++)
		{
			hashMinimumValues[i] = Integer.MAX_VALUE;
		}

		if (tokens == null || tokens.length == 0)
		{
			return hashMinimumValues;
		}

		// Go over all tokens and generate words (k-shingling)
		for (int tokenIndex = 0; tokenIndex < tokens.length; tokenIndex++)
		{
			// Build the word by concatenating consecutive tokens
			StringBuilder wordBuilder = new StringBuilder(tokensInWord);
			for (int i = tokenIndex; i < tokenIndex + tokensInWord; i++)
			{
				if (i < tokens.length)
				{
					wordBuilder.append(tokens[i]);
				}
				else
				{
					break;
				}
			}

			int hashCode = wordBuilder.toString().hashCode();

			// Go over all hash functions			
			for (int hashIndex = 0; hashIndex < numHashFunctions; hashIndex++)
			{
				// compute hash value of token with current hash function
				int hashValue = hashFunctions[hashIndex].calculateHash(hashCode);

				// Update minimum value at index hashIndex
				hashMinimumValues[hashIndex] = Math.min(hashMinimumValues[hashIndex], hashValue);
			}
		}

		// Return the MinHash Sketch
		return hashMinimumValues;
	}
	
	
	/**
	 * Compares two MinHash sketches
	 * @param firstMinHashSketch The first MinHash sketch to compare
	 * @param secondMinHashSketch The second MinHash sketch to compare
	 * @return Similarity result (between 0 and 1)
	 */
	public double compareSketches(int[] firstMinHashSketch, int[] secondMinHashSketch)
	{
		// count equal hashes
		int equalHashes = 0;
		for (int i = 0; i < numHashFunctions; i++)
		{
			if (firstMinHashSketch[i] == secondMinHashSketch[i])
			{
				equalHashes++;
			}
		}

		return (1.0 * equalHashes) / numHashFunctions; // similarity index
	}
}