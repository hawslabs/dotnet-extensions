public static class StringEx {
	extension(string? value) {
		public bool ContainsAny(
			string[] substrings,
			StringComparison comparison = StringComparison.OrdinalIgnoreCase
		) {
			if (value is null) {
				return false;
			}

			return substrings.Any(substring =>
				value.Contains(substring, comparison)
			);
		}
	}
}