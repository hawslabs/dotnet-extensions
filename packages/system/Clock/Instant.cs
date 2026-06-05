namespace System.Clock;

public readonly record struct Instant(long Ticks) : IComparable<Instant> {
	public DateTime Value => new(Ticks, DateTimeKind.Utc);

	public DateTimeOffset OffsetValue => new(Value);

	public static Instant MinValue { get; } =
		new(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc).Ticks);

	public static Instant MaxValue { get; } =
		new(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc).Ticks);

	public static Instant Now => FromDateTimeOffset(TimeProvider.System.GetUtcNow());

	public static Instant FromDateTime(
		DateTime value,
		UnspecifiedDateTimeHandling? unspecifiedHandling = null
	) {
		return new(value.ToUtc(unspecifiedHandling).Ticks);
	}

	public static Instant FromDateTimeOffset(DateTimeOffset value) {
		return new(value.UtcDateTime.Ticks);
	}

	public static Instant FromUnixTimeMilliseconds(long milliseconds) {
		return FromDateTimeOffset(DateTimeOffset.FromUnixTimeMilliseconds(milliseconds));
	}

	public long ToUnixTimeMilliseconds() {
		return OffsetValue.ToUnixTimeMilliseconds();
	}

	public ZonedInstant InZone(TimeZoneInfo timeZone) {
		return new(this, timeZone);
	}

	public Instant Add(TimeSpan duration) {
		return new(Value.Add(duration).Ticks);
	}

	public int CompareTo(Instant other) {
		return Ticks.CompareTo(other.Ticks);
	}

	public override string ToString() {
		return Value.ToString("O", CultureInfo.InvariantCulture);
	}

    public static implicit operator DateTime(Instant instant) => instant.Value;
    public static implicit operator DateTimeOffset(Instant instant) => instant.OffsetValue;
	public static bool operator <(Instant left, Instant right) => left.Ticks < right.Ticks;
	public static bool operator <=(Instant left, Instant right) => left.Ticks <= right.Ticks;
	public static bool operator >(Instant left, Instant right) => left.Ticks > right.Ticks;
	public static bool operator >=(Instant left, Instant right) => left.Ticks >= right.Ticks;
	public static TimeSpan operator -(Instant left, Instant right) => left.Value - right.Value;
	public static Instant operator +(Instant value, TimeSpan duration) => value.Add(duration);
	public static Instant operator -(Instant value, TimeSpan duration) => value.Add(-duration);
}