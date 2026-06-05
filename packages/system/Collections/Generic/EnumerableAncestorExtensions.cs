namespace System.Collections.Generic;

public static class EnumerableAncestorExtensions {
	public static T? FindCommonAncestor<T>(
		this IEnumerable<T> source,
		Func<T, T?> getParent,
		IEqualityComparer<T>? comparer = null
	)
		where T : class {
		comparer ??= EqualityComparer<T>.Default;

		using var enumerator = source.GetEnumerator();

		if (!enumerator.MoveNext()) {
			return null;
		}

		var ancestor = enumerator.Current;

		while (enumerator.MoveNext()) {
			ancestor = FindCommonAncestor(ancestor, enumerator.Current, getParent, comparer);

			if (ancestor is null) {
				return null;
			}
		}

		return ancestor;
	}

	private static T? FindCommonAncestor<T>(
		T left,
		T right,
		Func<T, T?> getParent,
		IEqualityComparer<T> comparer
	)
		where T : class {
		var leftAncestors = new HashSet<T>(comparer);

		for (var current = left; current is not null; current = getParent(current)) {
			leftAncestors.Add(current);
		}

		for (var current = right; current is not null; current = getParent(current)) {
			if (leftAncestors.Contains(current)) {
				return current;
			}
		}

		return null;
	}
}
