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
		var options = GlobsOptions.Default with {
			IncludePatterns = ["**/*.{cs,md}"],
			ExcludePatterns = [],
			PatternMode = GlobPatternMode.Fallback,
		};

		var files = temp.Root.Glob(options)
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
		var options = GlobsOptions.Default with {
			IncludePatterns = ["**/*.cs"],
			ExcludePatterns = ["**/bin/**"],
			PatternMode = GlobPatternMode.Fallback,
		};

		var files = temp.Root.Glob(options)
			.Select(file => file.GetRelativeNormalizedPath(basePath))
			.ToArray();

		files.Should().Equal("src/App.cs");
	}

	[Fact]
	public void Glob_OptionsIncludePatternIsSet_ReturnsMatchingFiles() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs");
		temp.CreateFile("docs/Guide.md");
		var basePath = temp.Root.FullName;
		var options = GlobsOptions.Default with {
			IncludePatterns = ["**/*.md"],
			ExcludePatterns = [],
			PatternMode = GlobPatternMode.Fallback,
		};

		var files = temp.Root.Glob(options: options)
			.Select(file => file.GetRelativeNormalizedPath(basePath))
			.ToArray();

		files.Should().Equal("docs/Guide.md");
	}

	[Fact]
	public void Glob_OptionsPatternsAreNotSet_FallsBackToDefaultPatterns() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs");
		temp.CreateFile("src/bin/Generated.cs");
		var basePath = temp.Root.FullName;

		var files = temp.Root.Glob(options: GlobsOptions.Default)
			.Select(file => file.GetRelativeNormalizedPath(basePath))
			.ToArray();

		files.Should().Equal("src/App.cs");
	}

	[Fact]
	public void Glob_PatternDefaultModeIsMerge_CombinesPatternsWithDefaults() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs");
		temp.CreateFile("docs/Guide.md");
		temp.CreateFile("notes/Todo.txt");
		var basePath = temp.Root.FullName;
		var options = GlobsOptions.Default with {
			DefaultIncludePatterns = ["**/*.cs"],
			IncludePatterns = ["**/*.md"],
			ExcludePatterns = [],
			PatternMode = GlobPatternMode.Merge,
		};

		var files = temp.Root.Glob(options: options)
			.Select(file => file.GetRelativeNormalizedPath(basePath))
			.ToArray();

		files.Should().Equal("docs/Guide.md", "src/App.cs");
	}

	[Fact]
	public void EffectiveIncludePatterns_PatternModeIsFallback_ReturnsConfiguredPatterns() {
		var options = GlobsOptions.Default with {
			DefaultIncludePatterns = ["**/*.cs"],
			IncludePatterns = ["**/*.md"],
			PatternMode = GlobPatternMode.Fallback,
		};

		options.EffectiveIncludePatterns.Should().Equal("**/*.md");
	}

	[Fact]
	public void EffectiveIncludePatterns_PatternContainsBraceExpansion_ReturnsExpandedPatterns() {
		var options = GlobsOptions.Default with {
			IncludePatterns = ["**/*.{cs,md}"],
			PatternMode = GlobPatternMode.Fallback,
		};

		options.EffectiveIncludePatterns.Should().Equal("**/*.cs", "**/*.md");
	}

	[Fact]
	public void EffectiveExcludePatterns_PatternModeIsMerge_CombinesConfiguredPatternsWithDefaults() {
		var options = GlobsOptions.Default with {
			DefaultExcludePatterns = ["**/bin/**"],
			ExcludePatterns = ["**/obj/**"],
			PatternMode = GlobPatternMode.Merge,
		};

		options.EffectiveExcludePatterns.Should().Equal("**/bin/**", "**/obj/**");
	}
}