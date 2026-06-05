namespace HawsLabs.Extensions.Tests.IO;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class FileInfoExtensionsTests {
	[Fact]
	public void GetSegments_FilePath_ReturnsFullPathSegments() {
		using var temp = new TestDirectory();
		var file = temp.CreateFile("src/Features/Thing.cs");

		file.GetSegments().Should().EndWith(["src", "Features", "Thing.cs"]);
	}

	[Fact]
	public void GetRelativeNormalizedPath_FileIsBelowBasePath_ReturnsForwardSlashRelativePath() {
		using var temp = new TestDirectory();
		var file = temp.CreateFile("src/Features/Thing.cs");

		file.GetRelativeNormalizedPath(temp.Root.FullName).Should().Be("src/Features/Thing.cs");
	}
}
