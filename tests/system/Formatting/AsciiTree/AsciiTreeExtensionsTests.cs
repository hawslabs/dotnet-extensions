namespace HawsLabs.Extensions.Tests.Formatting.AsciiTree;

using System.Formatting.AsciiTree;
using System.Globalization;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class AsciiTreeExtensionsTests {
	[Fact]
	public void ToAsciiTree_FilesAreUnderBasePath_ReturnsFormattedAsciiTree() {
		using var temp = new TestDirectory();
		var app = temp.CreateFile("src/App.cs", "label_app\nConsole.WriteLine();");
		var readme = temp.CreateFile("src/Readme.md", "# Readme");
		var files = new[] { readme, app };
		var rootName = Path.GetFileNameOrPath(temp.Root.FullName);

		var tree = files.ToAsciiTree(temp.Root.FullName, new AsciiTreeNodeFormatterOptions {
			ShowIcons = false,
			ShowLabels = false,
			ShowLineCounts = true,
			AlignColumns = false,
			Culture = CultureInfo.InvariantCulture,
		});

		tree.Should().Be(
			$"""
			{rootName}  3
			└── src  3
			    ├── App.cs  2
			    └── Readme.md  1

			"""
		);
	}
}