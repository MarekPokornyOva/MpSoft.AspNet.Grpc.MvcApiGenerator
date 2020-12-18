#region using
using System.Collections.Generic;
#endregion using

static class EnumerableHelpers
{
	internal static IEnumerable<T> AsEnumerable<T>(this T item)
	{
		yield return item;
	}
}
