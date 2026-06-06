namespace System.Collections.Generic;

public static class CollectionExtensions {
	public static void AddRange<T>(this ICollection<T> set, IEnumerable<T> values) {
		foreach (var value in values) {
			set.Add(value);
		}
	}
}
