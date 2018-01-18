using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Helpers
{
	public class CompareObject<T> : IEqualityComparer<T>
	{
		readonly Func<T, T, bool> getEquals;
		readonly Func<T, int> getHashCode;

		public CompareObject(Func<T, T, bool> equals, Func<T, int> hasCode)
		{
			getEquals = equals;
			getHashCode = hasCode;
		}


		public bool Equals(T x, T y)
		{
			return getEquals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return getHashCode(obj);
		}
	}
	
}
