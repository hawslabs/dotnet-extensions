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

		tree.Should().Be(
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

		tree.Should().Be(
			string.Join(
				Environment.NewLine,
				$"{rootName.PadRight(maxNameColumnWidth)}  L101",
				$"{"├── App.cs".PadRight(maxNameColumnWidth)}  L100",
				$"{"└── Readme.md".PadRight(maxNameColumnWidth)}  L  1",
				""
			)
		);
	}
}