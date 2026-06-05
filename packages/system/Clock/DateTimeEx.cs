using System.Clock;

public static class DateTimeEx {
	extension(DateTime value) {
		public DateTime ToUtc(
			UnspecifiedDateTimeHandling? unspecifiedHandling = null
		) {
			unspecifiedHandling ??= UnspecifiedDateTimeHandling.Throw;

			return value.Kind switch {
				DateTimeKind.Utc => value,
				DateTimeKind.Local => value.ToUniversalTime(),
				DateTimeKind.Unspecified => unspecifiedHandling.ToUtc(value),
				_ => throw new ArgumentOutOfRangeException(nameof(value)),
			};
		}
	}
}