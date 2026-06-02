namespace HawsLabs.Extensions.Tests.IO;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class GlobExtensionsTests {
	[Fact]
	public void Glob_IncludePatternUsesBraceExpansion_ReturnsMatchingFiles() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs");
		temp.CreateFile("docs/Guide.md");
		temp.CreateFile("src/App.txt");
		var basePath = temp.Root.FullName;

		var files = temp.Root.Glob(["**/*.{cs,md}"], [])
			.Select(file => file.GetRelativeNormalizedPath(basePath))
			.ToArray();

		files.Should().Equal("docs/Guide.md", "src/App.cs");
	}

	[Fact]
	public void Glob_ExcludePatternMatchesFile_RemovesFileFromResults() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs");
		temp.CreateFile("src/bin/Generated.cs");
		var basePath = temp.Root.FullName;

		var files = temp.Root.Glob(["**/*.cs"], ["**/bin/**"])
			.Select(file => file.GetRelativeNormalizedPath(basePath))
			.ToArray();

		files.Should().Equal("src/App.cs");
	}
}