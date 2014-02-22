using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace BloomFilterLib
{
	public static class ObjectUtils
	{
		/// <summary>
		/// Generate a hash value from an array of bits
		/// </summary>
		/// <remarks>voir http://blog.roblevine.co.uk for comparison of hash algorithm implementations</remarks>
		/// <param name="data">array of bits to hash</param>
		/// <returns></returns>
		public static int HashBytes( BitArray data )
		{
			// convert bit array to integer array
			int[] intArray = new int[(data.Length + 31) / 32];
			data.CopyTo(intArray, 0);
			// compute the hash from integer array values
			unchecked {
				int hash = 23;
				foreach (int n in intArray) {
					hash = hash * 37 + n;
				}
				return hash;
			}
		}

		/// <summary>
		/// Check if two arrays of bits are equals
		/// Returns true if every bit of this first array is equal to the corresponding bit of the second, false otherwise
		/// </summary>
		public static bool Equals( BitArray A, BitArray B )
		{
			if (A.Length != B.Length) return false;

			var enumA = A.GetEnumerator();
			var enumB = B.GetEnumerator();

			while (enumA.MoveNext() && enumB.MoveNext()) {
				if ((bool)enumA.Current != (bool)enumB.Current) return false;
			}
			return true;
		}

	}
}
