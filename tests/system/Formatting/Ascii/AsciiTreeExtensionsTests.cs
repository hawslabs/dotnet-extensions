namespace HawsLabs.Extensions.Tests.Formatting.Ascii;

using System.Globalization;
using System.Trees.Formatting.Ascii;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class AsciiTreeExtensionsTests {
	[Fact]
	public void ToAsciiTree_FilesShareCommonDirectory_ReturnsFormattedAsciiTree() {
		using var temp = new TestDirectory();
		var app = temp.CreateFile("src/App.cs", "label_app\nConsole.WriteLine();");
		var readme = temp.CreateFile("src/Readme.md", "# Readme");
		var files = new[] { readme, app };

		var tree = files.ToAsciiTree(new() {
			ShowIcons = false,
			ShowLabels = false,
			ShowLineCounts = true,
			AlignColumns = false,
			Culture = CultureInfo.InvariantCulture,
		});

		tree.RootPath.Should().Be(Path.Combine(temp.Root.FullName, "src"));
		tree.Text.Should().Be(
			$"""
			src  3
			├── App.cs  2
			└── Readme.md  1

			"""
		);
	}

	[Fact]
	public void ToAsciiTree_LineCountIconIsSet_IncludesIconWithLineCounts() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs", string.Join('\n', Enumerable.Range(1, 100).Select(i => $"Line {i}")));
		temp.CreateFile("src/Readme.md", "# Readme");
		const string rootName = "src";

		var tree = temp.Root.GetFiles("*", SearchOption.AllDirectories).ToAsciiTree(new() {
			ShowIcons = false,
			ShowLabels = false,
			ShowLineCounts = true,
			LineCountIcon = "L",
			AlignColumns = true,
			Culture = CultureInfo.InvariantCulture,
		});
		var maxNameColumnWidth = Math.Max(rootName.Length, "└── Readme.md".Length);

		tree.RootPath.Should().Be(Path.Combine(temp.Root.FullName, "src"));
		tree.Text.Should().Be(
			string.Join(
				Environment.NewLine,
				$"{rootName.PadRight(maxNameColumnWidth)}  L101",
				$"{"├── App.cs".PadRight(maxNameColumnWidth)}  L100",
				$"{"└── Readme.md".PadRight(maxNameColumnWidth)}  L  1",
				""
			)
		);
	}

	[Fact]
	public void ToAsciiTree_RootPathIsProvided_UsesProvidedRootPath() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs", "Console.WriteLine();");
		temp.CreateFile("tests/AppTests.cs", "Assert.True(true);");

		var tree = temp.Root.GetFiles("*", SearchOption.AllDirectories).ToAsciiTree(new() {
			ShowIcons = false,
			ShowLabels = false,
			ShowLineCounts = false,
			AlignColumns = false,
		});

		tree.RootPath.Should().Be(temp.Root.FullName);
		tree.Text.Should().Be(
			$"""
			{Path.GetFileNameOrPath(temp.Root.FullName)}
			├── src
			│   └── App.cs
			└── tests
			    └── AppTests.cs

			"""
		);
	}
}