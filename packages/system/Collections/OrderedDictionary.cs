namespace System.Collections;

public sealed class OrderedDictionary<TKey, TValue>(
	IEqualityComparer<TKey> comparer
) : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull {
	private readonly List<KeyValuePair<TKey, TValue>> list = [];
	private readonly Dictionary<TKey, int> index = new(comparer);

	public TValue this[TKey key] {
		get => list[index[key]].Value;
		set {
			if (index.TryGetValue(key, out var existingIndex)) {
				list[existingIndex] = new(key, value);
				return;
			}

			index[key] = list.Count;
			list.Add(new(key, value));
		}
	}

	public IEnumerable<TValue> Values => list.Select(kv => kv.Value);

	public bool TryGetValue(TKey key, out TValue value) {
		if (index.TryGetValue(key, out var existingIndex)) {
			value = list[existingIndex].Value;
			return true;
		}

		value = default!;
		return false;
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
		return list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}