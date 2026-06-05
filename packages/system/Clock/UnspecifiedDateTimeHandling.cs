namespace System.Clock;

public abstract class UnspecifiedDateTimeHandling {
	public static readonly UnspecifiedDateTimeHandling Throw = new ThrowUnspecifiedDateTimeHandling();
	public static readonly UnspecifiedDateTimeHandling AssumeUtc = new AssumeUtcUnspecifiedDateTimeHandling();
	public static readonly UnspecifiedDateTimeHandling AssumeLocal = new AssumeLocalUnspecifiedDateTimeHandling();

	public abstract DateTime ToUtc(DateTime value);

	private sealed class ThrowUnspecifiedDateTimeHandling : UnspecifiedDateTimeHandling {
		public override DateTime ToUtc(DateTime value) {
			throw new ArgumentException(
				"DateTime.Kind is Unspecified. Specify whether to assume UTC or local time.",
				nameof(value)
			);
		}
	}

	private sealed class AssumeUtcUnspecifiedDateTimeHandling : UnspecifiedDateTimeHandling {
		public override DateTime ToUtc(DateTime value) {
			return DateTime.SpecifyKind(value, DateTimeKind.Utc);
		}
	}

	private sealed class AssumeLocalUnspecifiedDateTimeHandling : UnspecifiedDateTimeHandling {
		public override DateTime ToUtc(DateTime value) {
			return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
		}
	}
}