public static class DateTimeEx {
    extension (DateTime value) {
		public DateTime EnsureUtc() {
			return value.Kind switch {
				DateTimeKind.Utc => value,
				DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
				DateTimeKind.Local => value.ToUniversalTime(),
				_ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
			};
		}
	}
}