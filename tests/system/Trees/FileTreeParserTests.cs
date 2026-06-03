namespace HawsLabs.Extensions.Tests.Trees;

using System.Trees.Nodes;
using System.Trees.Parsing;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class FileTreeParserTests {
	[Fact]
	public void Parse_FilesAreUnderBasePath_BuildsFolderAndFileNodes() {
		using var temp = new TestDirectory();
		var app = temp.CreateFile("src/App.cs", "label_app\nConsole.WriteLine();");
		var readme = temp.CreateFile("src/Readme.md", "# Readme");

		var root = FileTreeParser.Parse(
			[readme, app],
			new() {
				BasePath = temp.Root.FullName,
			}
		);

		root.Name.Should().Be(Path.GetFileNameOrPath(temp.Root.FullName));
		root.TotalLines.Should().Be(3);
		root.TotalLabelCount.Should().Be(1);

		var src = root.Children["src"].Should().BeOfType<FolderTreeNode>().Subject;
		src.TotalLines.Should().Be(3);
		src.TotalLabelCount.Should().Be(1);

		var appNode = src.Children["App.cs"].Should().BeOfType<FileTreeNode>().Subject;
		appNode.File.FullName.Should().Be(app.FullName);
		appNode.FullPath.Should().Be(app.FullName);
		appNode.RelativePath.Should().Be("src/App.cs");
		appNode.LineCount.Should().Be(2);
		appNode.LabelCount.Should().Be(1);

		var readmeNode = src.Children["Readme.md"].Should().BeOfType<FileTreeNode>().Subject;
		readmeNode.RelativePath.Should().Be("src/Readme.md");
		readmeNode.LineCount.Should().Be(1);
		readmeNode.LabelCount.Should().Be(0);
	}

	[Fact]
	public void Parse_ReadFileMetricsIsFalse_CreatesFilesWithZeroMetrics() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs", "label_app\nConsole.WriteLine();");

		var root = FileTreeParser.Parse(
			temp.Root.GetFiles("*", SearchOption.AllDirectories),
			new() {
				BasePath = temp.Root.FullName,
				ReadFileMetrics = false,
			}
		);

		var src = root.Children["src"].Should().BeOfType<FolderTreeNode>().Subject;
		var app = src.Children["App.cs"].Should().BeOfType<FileTreeNode>().Subject;

		app.LineCount.Should().Be(0);
		app.LabelCount.Should().Be(0);
		root.TotalLines.Should().Be(0);
		root.TotalLabelCount.Should().Be(0);
	}

	[Fact]
	public void Parse_IgnoreFilesOutsideBasePathIsTrue_SkipsOutsideFiles() {
		using var temp = new TestDirectory();
		using var other = new TestDirectory();
		var inside = temp.CreateFile("src/App.cs");
		var outside = other.CreateFile("src/Other.cs");

		var root = FileTreeParser.Parse(
			[inside, outside],
			new() {
				BasePath = temp.Root.FullName,
				IgnoreFilesOutsideBasePath = true,
			}
		);

		var src = root.Children["src"].Should().BeOfType<FolderTreeNode>().Subject;

		src.Children.Values.Should().ContainSingle()
			.Which.Should().BeOfType<FileTreeNode>()
			.Which.File.FullName.Should().Be(inside.FullName);
	}

	[Fact]
	public void Parse_IgnoreFileReadErrorsIsTrue_UsesZeroMetricsForUnreadableFile() {
		using var temp = new TestDirectory();
		temp.CreateFile("src/App.cs", "Console.WriteLine();");

		var root = FileTreeParser.Parse(
			temp.Root.GetFiles("*", SearchOption.AllDirectories),
			new() {
				BasePath = temp.Root.FullName,
				IgnoreFileReadErrors = true,
				ReadLines = _ => throw new IOException("Cannot read test file."),
			}
		);

		var src = root.Children["src"].Should().BeOfType<FolderTreeNode>().Subject;
		var app = src.Children["App.cs"].Should().BeOfType<FileTreeNode>().Subject;

		app.LineCount.Should().Be(0);
		root.TotalLines.Should().Be(0);
	}

	[Fact]
	public void GetOrAddFolder_ChildNameDiffersOnlyByCase_ReturnsExistingFolder() {
		var root = new FolderTreeNode("root");
		var folder = root.GetOrAddFolder("src");

		var sameFolder = root.GetOrAddFolder("SRC");

		sameFolder.Should().BeSameAs(folder);
		root.Children.Values.Should().ContainSingle();
	}
}