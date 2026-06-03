namespace System.Collections;

public sealed class OrderedDictionary<TKey, TValue>(
	IEqualityComparer<TKey> comparer
) : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull {
	private readonly List<KeyValuePair<TKey, TValue>> _list = [];
	private readonly Dictionary<TKey, int> _index = new(comparer);

	public TValue this[TKey key] {
		get => _list[_index[key]].Value;
		set {
			if (_index.TryGetValue(key, out var existingIndex)) {
				_list[existingIndex] = new(key, value);
				return;
			}

			_index[key] = _list.Count;
			_list.Add(new(key, value));
		}
	}

	public IEnumerable<TValue> Values => _list.Select(kv => kv.Value);

	public bool TryGetValue(TKey key, out TValue value) {
		if (_index.TryGetValue(key, out var existingIndex)) {
			value = _list[existingIndex].Value;
			return true;
		}

		value = default!;
		return false;
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
		return _list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}