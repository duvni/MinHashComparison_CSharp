import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;

public class MinHashSimilarity {
	
	// The internal min hash instance
	private MinHash minHash;
	
	// The threshold in which documents are considered similar
	private double threshold;
	
	// Number of bands for LSH comparison
	private int bands;
	
	// number of rows for LSH comparison
	private int rows;
	
	// Buckets for LSH comparison
	private HashMap<String, List<int[]>> buckets;
	
	/**
	 * Default Constructor
	 * Works best if threshold is ~90%
	 * @param threashold The threshold in which documents are considered similar
	 * @throws Exception if an illegal value is used for one of the input parameters
	 */
	public MinHashSimilarity(double threshold) throws Exception
	{
		this (threshold, 5, 400, 20, 20);
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
	
	/**
	 * Constructor
	 * @param threshold The threshold in which documents are considered similar
	 * @param tokensInWord Number of tokens for a word
	 * @param numHashFunctions Number of min hash functions to compute
	 * @param bands Number of bands for LSH comparison
	 * @param rows Number of rows for LSH comparison
	 * @throws Exception if an illegal value is used for one of the input parameters
	 */
	public MinHashSimilarity(double threshold, int tokensInWord, int numHashFunctions, int bands, int rows) throws Exception
	{
		if (threshold < 0 || threshold > 100)
		{
			throw new Exception(String.format("MinHashSimilarity - Illegal threshold: %d", threshold));
		}
		
		if (bands*rows != numHashFunctions)
		{
			throw new Exception("MinHashSimilarity - bands * rows != numHashFunctions");
		}

		this.threshold = threshold;
		this.minHash = new MinHash(tokensInWord, numHashFunctions);
		this.bands = bands;
		this.rows = rows;
		this.buckets = new HashMap<String, List<int[]>>();
	}
	
	/**
	 * Clears all history of documents
	 */
	public void ClearDocuments()
	{
		buckets.clear();
	}
	
	/**
	 * Given a string document, looks whether a similar document was already seen
	 * @param doc The new document to compare to
	 * @return true if a similar document was already seen, false otherwise
	 */
	public boolean LookForSimilarDocument(String doc)
	{
		int[] minHashes = minHash.computeSketch(doc.split("\\s+"));
		String[] bandHashes = new String[bands];
		HashSet<int[]> comparedSketches = new HashSet<int[]>();

		for (int i = 0; i < bands; i++)
		{
			bandHashes[i] = computeBandHash(minHashes, i);
			
			if (buckets.containsKey(bandHashes[i]))
			{
				for (int[] sketchToCompare : buckets.get(bandHashes[i]))
				{
					if (!comparedSketches.contains(sketchToCompare))
					{
						if (minHash.compareSketches(minHashes, sketchToCompare) >= threshold)
						{
							// Found a similar document
							return true;
						}

						// Avoid comparing two documents twice
						comparedSketches.add(sketchToCompare);
					}
				}
			}
		}

		// No match found, add document to buckets
		for (int i = 0; i < bands; i++)
		{
			if (!buckets.containsKey(bandHashes[i]))
			{
				buckets.put(bandHashes[i], new ArrayList<int[]>());
			}
			buckets.get(bandHashes[i]).add(minHashes);
		}

		return false;
	}
	
	/**
	 * Computes a hash for quick bucket match search
	 * @param minHashes The MinHashes for row values
	 * @param i The ith band
	 * @return The computed hash for the ith band
	 */
	private String computeBandHash(int[] minHashes, int i)
	{
		StringBuilder bandHashSB = new StringBuilder((rows + 1) * 10);
		for (int j = 0; j < rows; j++)
		{
			// adding the rows corresponding to ith band
			bandHashSB.append(String.format("%010d", minHashes[i * rows + j]));
		}
		// adding the number i to distinguish between bands
		bandHashSB.append(String.format("%010d", i));
		return bandHashSB.toString();
	}
}