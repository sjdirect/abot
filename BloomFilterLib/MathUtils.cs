using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BloomFilterLib
{
	static class MathUtils
	{
		/// <summary>
		/// Compute the absolute value of an integer
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static int Abs( int n )
		{
			if (n >= 0) return n;
			else return -n;
		}
	}
}
