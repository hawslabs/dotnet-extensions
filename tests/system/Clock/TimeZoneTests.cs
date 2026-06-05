namespace HawsLabs.Extensions.Tests.Clock;

using System.Clock;
using FluentAssertions;
using Xunit;

public sealed class TimeZoneTests {
	private static readonly DateTimeOffset FixedUtc = new(2026, 6, 5, 18, 30, 0, TimeSpan.Zero);
	private static readonly TimeZoneId Eastern = TimeZoneId.From(WindowsTimeZoneId.From("Eastern Standard Time"));
	private static readonly WindowsTimeZoneId Pacific = WindowsTimeZoneId.From("Pacific Standard Time");
	private static readonly IanaTimeZoneId Tokyo = IanaTimeZoneId.From("Asia/Tokyo");

	[Fact]
	public void TimeZones_ReturnsTimeZoneIds() {
		TimeZoneInfo.TimeZones.Should().NotBeEmpty();
		TimeZoneInfo.TimeZones.Select(id => id.Value).Should().Equal(TimeZoneId.All.Select(id => id.Value));
	}

	[Fact]
	public void WindowsTimeZones_ReturnsWindowsTimeZoneIds() {
		TimeZoneInfo.WindowsTimeZones.Should().NotBeEmpty();
		TimeZoneInfo.WindowsTimeZones.Select(id => id.Value).Should().Equal(WindowsTimeZoneId.All.Select(id => id.Value));
	}

	[Fact]
	public void IanaTimeZones_ReturnsIanaTimeZoneIds() {
		TimeZoneInfo.IanaTimeZones.Should().NotBeEmpty();
		TimeZoneInfo.IanaTimeZones.Select(id => id.Value).Should().Equal(IanaTimeZoneId.All.Select(id => id.Value));
	}

	[Fact]
	public void All_TimeZoneIds_ReturnsResolvableTimeZones() {
		TimeZoneId.All.Should().NotBeEmpty();
		TimeZoneId.All.Select(id => id.Value).Should().OnlyHaveUniqueItems();
		TimeZoneId.All.Select(id => id.Info).ToArray().Should().HaveSameCount(TimeZoneId.All);
	}

	[Fact]
	public void All_WindowsTimeZoneIds_ReturnsResolvableTimeZones() {
		WindowsTimeZoneId.All.Should().NotBeEmpty();
		WindowsTimeZoneId.All.Select(id => id.Value).Should().OnlyHaveUniqueItems();
		WindowsTimeZoneId.All.Select(id => id.Info).ToArray().Should().HaveSameCount(WindowsTimeZoneId.All);
	}

	[Fact]
	public void All_IanaTimeZoneIds_ReturnsResolvableTimeZones() {
		IanaTimeZoneId.All.Should().NotBeEmpty();
		IanaTimeZoneId.All.Select(id => id.Value).Should().OnlyHaveUniqueItems();
		IanaTimeZoneId.All.Select(id => id.Info).ToArray().Should().HaveSameCount(IanaTimeZoneId.All);
	}

	[Fact]
	public void WithTimeZone_TimeZoneId_ReturnsZonedInstant() {
		var dateTime = FixedUtc.UtcDateTime;

		var zoned = dateTime.WithTimeZone(Eastern);

		zoned.Instant.Should().Be(Instant.FromDateTimeOffset(FixedUtc));
		zoned.TimeZoneId.Should().Be(Eastern.Value);
	}

	[Fact]
	public void WithTimeZone_WindowsTimeZoneId_ReturnsZonedInstant() {
		var dateTime = FixedUtc.UtcDateTime;

		var zoned = dateTime.WithTimeZone(Pacific);

		zoned.Instant.Should().Be(Instant.FromDateTimeOffset(FixedUtc));
		zoned.TimeZoneId.Should().Be(Pacific.Value);
	}

	[Fact]
	public void WithTimeZone_IanaTimeZoneId_ReturnsZonedInstant() {
		var dateTime = FixedUtc.UtcDateTime;

		var zoned = dateTime.WithTimeZone(Tokyo);

		zoned.Instant.Should().Be(Instant.FromDateTimeOffset(FixedUtc));
		zoned.TimeZoneId.Should().Be(Tokyo.Value);
	}

	[Fact]
	public void ToTimeZone_TimeZoneId_PreservesInstant() {
		var instant = Instant.FromDateTimeOffset(FixedUtc);
		var zoned = ZonedInstant.FromInstant(instant, TimeZoneInfo.Utc);

		var converted = zoned.ToTimeZone(Eastern);

		converted.Instant.Should().Be(instant);
		converted.TimeZoneId.Should().Be(Eastern.Value);
	}

	[Fact]
	public void ToTimeZone_WindowsTimeZoneId_PreservesInstant() {
		var instant = Instant.FromDateTimeOffset(FixedUtc);
		var zoned = ZonedInstant.FromInstant(instant, TimeZoneInfo.Utc);

		var converted = zoned.ToTimeZone(Pacific);

		converted.Instant.Should().Be(instant);
		converted.TimeZoneId.Should().Be(Pacific.Value);
	}

	[Fact]
	public void ToTimeZone_IanaTimeZoneId_PreservesInstant() {
		var instant = Instant.FromDateTimeOffset(FixedUtc);
		var zoned = ZonedInstant.FromInstant(instant, TimeZoneInfo.Utc);

		var converted = zoned.ToTimeZone(Tokyo);

		converted.Instant.Should().Be(instant);
		converted.TimeZoneId.Should().Be(Tokyo.Value);
	}

	[Fact]
	public void GetInstantInTimeZone_TimeZoneId_ReturnsProviderInstant() {
		var provider = new FixedTimeProvider(FixedUtc);

		var zoned = provider.GetInstantInTimeZone(Eastern);

		zoned.Instant.Should().Be(Instant.FromDateTimeOffset(FixedUtc));
		zoned.TimeZoneId.Should().Be(Eastern.Value);
	}

	[Fact]
	public void GetInstantInTimeZone_WindowsTimeZoneId_ReturnsProviderInstant() {
		var provider = new FixedTimeProvider(FixedUtc);

		var zoned = provider.GetInstantInTimeZone(Pacific);

		zoned.Instant.Should().Be(Instant.FromDateTimeOffset(FixedUtc));
		zoned.TimeZoneId.Should().Be(Pacific.Value);
	}

	[Fact]
	public void GetInstantInTimeZone_IanaTimeZoneId_ReturnsProviderInstant() {
		var provider = new FixedTimeProvider(FixedUtc);

		var zoned = provider.GetInstantInTimeZone(Tokyo);

		zoned.Instant.Should().Be(Instant.FromDateTimeOffset(FixedUtc));
		zoned.TimeZoneId.Should().Be(Tokyo.Value);
	}

	private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider {
		public override DateTimeOffset GetUtcNow() {
			return value;
		}
	}
}