using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace BloomFilterLib
{
    /// <summary>
    /// Implementation of a Bloom-filter, as described here:
    /// http://en.wikipedia.org/wiki/Bloom_filter
    /// 
    /// Translated from Java to C# 4.0. Original Java Source (by Magnus Skjegstad) : http://github.com/magnuss/java-bloomfilter
    /// <remarks>
    ///		author Magnus Skjegstad (magnus@skjegstad.com)
    ///		translated into .NET 4.0 by Jeckyhl (@sourceforge)
    ///		Modified by Ilya Beyrak to utilize MurMur3 Algorithim for hasing
    /// </remarks> 
    /// </summary>
    [Serializable]
    public class BloomFilter
    {
        private BitArray bitset;
        private int bitSetSize;
        private double bitsPerElement;
        private int expectedNumberOfFilterElements; // expected (maximum) number of elements to be added
        private int numberOfAddedElements; // number of elements actually added to the Bloom filter
        private int k; // number of hash functions

        static readonly Encoding charset = Encoding.UTF8; // encoding used for storing hash values as strings


        /// <summary>
        /// Constructs an empty Bloom filter. The total length of the Bloom filter will be
        /// c*n.
        /// </summary>
        /// <param name="c">is the number of bits used per element.</param>
        /// <param name="n">is the expected number of elements the filter will contain.</param>
        /// <param name="k">is the number of hash functions used.</param>
        public BloomFilter(double c, int n, int k)
        {
            this.expectedNumberOfFilterElements = n;
            this.k = k;
            this.bitsPerElement = c;
            this.bitSetSize = Math.Ceiling(c * n) > int.MaxValue ? int.MaxValue : (int)Math.Ceiling(c * n);
            numberOfAddedElements = 0;
            this.bitset = new BitArray(bitSetSize);
        }

        /// <summary>
        /// Constructs an empty Bloom filter. The optimal number of hash functions (k) is estimated from the total size of the Bloom
        /// and the number of expected elements.
        /// </summary>
        /// <param name="bitSetSize">defines how many bits should be used in total for the filter.</param>
        /// <param name="expectedNumberOElements">defines the maximum number of elements the filter is expected to contain.</param>
        public BloomFilter(int bitSetSize, int expectedNumberOElements)
            : this(
                bitSetSize / (double)expectedNumberOElements,
                expectedNumberOElements,
                (int)Math.Round((bitSetSize / (double)expectedNumberOElements) * Math.Log(2.0))
              )
        {


        }

        public static TData DeserializeFromString<TData>(string settings)
        {
            byte[] b = Convert.FromBase64String(settings);

            using (var stream = new MemoryStream(b))
            {
                using (var tinyStream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    var formatter = new BinaryFormatter();
                    return (TData)formatter.Deserialize(tinyStream);
                }
            }
        }

        public static string SerializeToString<TData>(TData settings)
        {
            
            using (var stream = new MemoryStream())
            {
                using(var tinyStream = new GZipStream(stream,CompressionMode.Compress))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(tinyStream, settings);
                }
                return Convert.ToBase64String(stream.ToArray());
            }
        }

       
        public string BloomFilterBinarySerialization()
        {
            
            return SerializeToString<BloomFilter>(this);
        }
        /// <summary>
        /// Constructs an empty Bloom filter with a given false positive probability. The number of bits per
        /// element and the number of hash functions is estimated
        /// to match the false positive probability.
        /// </summary>
        /// <param name="falsePositiveProbability">is the desired false positive probability.</param>
        /// <param name="expectedNumberOfElements">is the expected number of elements in the Bloom filter.</param>
        public BloomFilter(double falsePositiveProbability, int expectedNumberOfElements)
            : this(
                Math.Ceiling(-(Math.Log(falsePositiveProbability) / Math.Log(2))) / Math.Log(2), // c = k / ln(2)
                expectedNumberOfElements,
                (int)Math.Ceiling(-(Math.Log(falsePositiveProbability) / Math.Log(2))) // k = ceil(-log_2(false prob.))
              ) { }

        /// <summary>
        /// Construct a new Bloom filter based on existing Bloom filter data.
        /// </summary>
        /// <param name="bitSetSize">defines how many bits should be used for the filter.</param>
        /// <param name="expectedNumberOfFilterElements">defines the maximum number of elements the filter is expected to contain.</param>
        /// <param name="actualNumberOfFilterElements">specifies how many elements have been inserted into the <code>filterData</code> BitArray.</param>
        /// <param name="filterData">a BitArray representing an existing Bloom filter.</param>
        public BloomFilter(int bitSetSize, int expectedNumberOfFilterElements, int actualNumberOfFilterElements, BitArray filterData)
            : this(bitSetSize, expectedNumberOfFilterElements)
        {
            this.bitset = filterData;
            this.numberOfAddedElements = actualNumberOfFilterElements;
        }

        /// <summary>
        /// Generates a digest based on the contents of a string.
        /// </summary>
        /// <param name="val">specifies the input data.</param>
        /// <param name="charset">specifies the encoding of the input data.</param>
        /// <returns>digest as long.</returns>
        public static int createHash(string val, Encoding charset)
        {
            return createHash(charset.GetBytes(val));
        }

        /// <summary>
        /// Generates a digest based on the contents of a string.
        /// </summary>
        /// <param name="val">specifies the input data. The encoding is expected to be UTF-8.</param>
        /// <returns>digest as long.</returns>
        public static int createHash(string val)
        {
            return createHash(val, charset);
        }

        /// <summary>
        /// Generates a digest based on the contents of an array of bytes.
        /// </summary>
        /// <param name="data">specifies input data.</param>
        /// <returns>digest as long.</returns>
        public static int createHash(byte[] data)
        {
            return createHashes(data, 1)[0];
        }

        /// <summary>
        /// Generates digests based on the contents of an array of bytes and splits the result into 4-byte int's and store them in an array. The
        /// digest function is called until the required number of int's are produced. For each call to digest a salt
        /// is prepended to the data. The salt is increased by 1 for each call.
        /// </summary>
        /// <param name="data">specifies input data</param>
        /// <param name="hashes">number of hashes/int's to produce</param>
        /// <returns>array of int-sized hashes</returns>
        public static int[] createHashes(byte[] data, int hashes)
        {


            int[] result = new int[hashes];

            int k = 0;
            uint salt = 0;
            while (k < hashes)
            {
                byte[] digest;
                MurMur3.Murmur3 mm3 = new MurMur3.Murmur3(salt);
                salt++;
                digest = mm3.ComputeHash(data);

                for (int i = 0; i < digest.Length / 4 && k < hashes; i++)
                {
                    int h = 0;
                    for (int j = (i * 4); j < (i * 4) + 4; j++)
                    {
                        h <<= 8;
                        h |= ((int)digest[j]) & 0xFF;
                    }
                    result[k] = h;
                    k++;
                }
            }
            return result;
        }

        /// <summary>
        /// Compares the contents of two instances to see if they are equal.
        /// </summary>
        /// <param name="obj">is the object to compare to.</param>
        /// <returns>True if the contents of the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            BloomFilter other = (BloomFilter)obj;
            if (this.expectedNumberOfFilterElements != other.expectedNumberOfFilterElements)
            {
                return false;
            }
            if (this.k != other.k)
            {
                return false;
            }
            if (this.bitSetSize != other.bitSetSize)
            {
                return false;
            }
            if (this.bitset != other.bitset && (this.bitset == null || !ObjectUtils.Equals(this.bitset, other.bitset)))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Calculates a hash code for this class.
        /// <remarks>performance concerns : note that we read all the bits of bitset to compute the hash</remarks>
        /// <returns>hash code representing the contents of an instance of this class.</returns>
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 7;
            hash = 61 * hash + (this.bitset != null ? ObjectUtils.HashBytes(this.bitset) : 0);
            hash = 61 * hash + this.expectedNumberOfFilterElements;
            hash = 61 * hash + this.bitSetSize;
            hash = 61 * hash + this.k;
            return hash;
        }


        /// <summary>
        /// Calculates the expected probability of false positives based on
        /// the number of expected filter elements and the size of the Bloom filter.
        /// <br /><br />
        /// The value returned by this method is the <i>expected</i> rate of false
        /// positives, assuming the number of inserted elements equals the number of
        /// expected elements. If the number of elements in the Bloom filter is less
        /// than the expected value, the true probability of false positives will be lower.
        /// </summary>
        /// <returns>expected probability of false positives.</returns>
        public double expectedFalsePositiveProbability()
        {
            return getFalsePositiveProbability(expectedNumberOfFilterElements);
        }

        /// <summary>
        /// Calculate the probability of a false positive given the specified
        /// number of inserted elements.
        /// </summary>
        /// <param name="numberOfElements">number of inserted elements.</param>
        /// <returns>probability of a false positive.</returns>
        public double getFalsePositiveProbability(double numberOfElements)
        {
            // (1 - e^(-k * n / m)) ^ k
            return Math.Pow((1 - Math.Exp(-k * (double)numberOfElements
                        / (double)bitSetSize)), k);

        }

        /// <summary>
        /// Get the current probability of a false positive. The probability is calculated from
        /// the size of the Bloom filter and the current number of elements added to it.
        /// </summary>
        /// <returns>probability of false positives.</returns>
        public double getFalsePositiveProbability()
        {
            return getFalsePositiveProbability(numberOfAddedElements);
        }


        /// <summary>
        /// Returns the value chosen for K.<br />
        /// <br />
        /// K is the optimal number of hash functions based on the size
        /// of the Bloom filter and the expected number of inserted elements.
        /// </summary>
        /// <returns>optimal k.</returns>
        public int getK()
        {
            return k;
        }

        /// <summary>
        /// Sets all bits to false in the Bloom filter.
        /// </summary>
        public void clear()
        {
            bitset.SetAll(false);
            numberOfAddedElements = 0;
        }

        /// <summary>
        /// Adds an object to the Bloom filter. The output from the object's
        /// ToString() method is used as input to the hash functions.
        /// </summary>
        /// <param name="element">is an element to register in the Bloom filter.</param>
        public void add(object element)
        {
            add(charset.GetBytes(element.ToString()));
        }

        /// <summary>
        /// Adds an array of bytes to the Bloom filter.
        /// </summary>
        /// <param name="bytes">array of bytes to add to the Bloom filter.</param>
        public void add(byte[] bytes)
        {
            int[] hashes = createHashes(bytes, k);
            foreach (int hash in hashes)
            {
                bitset.Set(MathUtils.Abs(hash % bitSetSize), true);
            }
            numberOfAddedElements++;
        }

        /// <summary>
        /// Adds all elements from a Collection to the Bloom filter.
        /// </summary>
        /// <param name="c">Collection of elements.</param>
        public void addAll(IEnumerable<object> c)
        {
            foreach (object element in c)
            {
                add(element);
            }
        }

        /// <summary>
        /// Adds all elements from a Collection to the Bloom filter.
        /// </summary>
        /// <param name="c">Collection of elements.</param>
        public void addAll(IEnumerable<byte[]> c)
        {
            foreach (byte[] byteArray in c)
            {
                add(byteArray);
            }
        }

        /// <summary>
        /// Returns true if the element could have been inserted into the Bloom filter.
        /// Use getFalsePositiveProbability() to calculate the probability of this
        /// being correct.
        /// </summary>
        /// <param name="element">element to check.</param>
        /// <returns>true if the element could have been inserted into the Bloom filter.</returns>
        public bool contains(object element)
        {
            return contains(charset.GetBytes(element.ToString()));
        }

        /// <summary>
        /// Returns true if the array of bytes could have been inserted into the Bloom filter.
        /// Use getFalsePositiveProbability() to calculate the probability of this
        /// being correct.
        /// </summary>
        /// <param name="bytes">array of bytes to check.</param>
        /// <returns>true if the array could have been inserted into the Bloom filter.</returns>
        public bool contains(byte[] bytes)
        {
            int[] hashes = createHashes(bytes, k);
            foreach (int hash in hashes)
            {
                if (!bitset.Get(MathUtils.Abs(hash % bitSetSize)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if all the elements of a Collection could have been inserted
        /// into the Bloom filter. Use getFalsePositiveProbability() to calculate the
        /// probability of this being correct.
        /// </summary>
        /// <param name="c">elements to check.</param>
        /// <returns>true if all the elements in c could have been inserted into the Bloom filter.</returns>
        public bool containsAll(IEnumerable<object> c)
        {
            foreach (object element in c)
            {
                if (!contains(element)) return false;
            }
            return true;
        }

        /// <summary>
        /// Read a single bit from the Bloom filter.
        /// </summary>
        /// <param name="bit">the bit to read.</param>
        /// <returns>true if the bit is set, false if it is not.</returns>
        public bool getBit(int bit)
        {
            return bitset.Get(bit);
        }

        /// <summary>
        /// Set a single bit in the Bloom filter.
        /// </summary>
        /// <param name="bit">is the bit to set.</param>
        /// <param name="value">If true, the bit is set. If false, the bit is cleared.</param>
        public void setBit(int bit, bool value)
        {
            bitset.Set(bit, value);
        }

        /// <summary>
        /// Return the bit set used to store the Bloom filter.
        /// </summary>
        /// <returns>bit set representing the Bloom filter.</returns>
        public BitArray getBitSet()
        {
            return bitset;
        }

        /// <summary>
        /// Returns the number of bits in the Bloom filter. Use count() to retrieve
        /// the number of inserted elements.
        /// </summary>
        /// <returns>the size of the bitset used by the Bloom filter.</returns>
        public int size()
        {
            return this.bitSetSize;
        }

        /// <summary>
        /// Returns the number of elements added to the Bloom filter after it
        /// was constructed or after clear() was called.
        /// </summary>
        /// <returns>number of elements added to the Bloom filter.</returns>
        public int count()
        {
            return this.numberOfAddedElements;
        }

        /// <summary>
        /// Returns the expected number of elements to be inserted into the filter.
        /// This value is the same value as the one passed to the constructor.
        /// </summary>
        /// <returns>expected number of elements.</returns>
        public int getExpectedNumberOfElements()
        {
            return expectedNumberOfFilterElements;
        }

        /// <summary>
        /// Get expected number of bits per element when the Bloom filter is full. This value is set by the constructor
        /// when the Bloom filter is created. See also getBitsPerElement().
        /// </summary>
        /// <returns>expected number of bits per element.</returns>
        public double getExpectedBitsPerElement()
        {
            return this.bitsPerElement;
        }

        /// <summary>
        /// Get actual number of bits per element based on the number of elements that have currently been inserted and the length
        /// of the Bloom filter. See also getExpectedBitsPerElement().
        /// </summary>
        /// <returns>number of bits per element.</returns>
        public double getBitsPerElement()
        {
            return this.bitSetSize / (double)numberOfAddedElements;
        }
    }
}
