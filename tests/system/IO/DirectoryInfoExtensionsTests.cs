namespace HawsLabs.Extensions.Tests.IO;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class DirectoryInfoExtensionsTests {
	[Fact]
	public void GetSegments_DirectoryPath_ReturnsFullPathSegments() {
		using var temp = new TestDirectory();
		var directory = temp.CreateDirectory("src/Features");

		directory.GetSegments().Should().EndWith(["src", "Features"]);
	}
}
