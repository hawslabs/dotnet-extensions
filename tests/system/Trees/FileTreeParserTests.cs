namespace HawsLabs.Extensions.Tests.Trees;

using ArchiveNodes = System.Trees.Nodes.Archive;
using FileSystemNodes = System.Trees.Nodes.FileSystem;
using MSBuildNodes = System.Trees.Nodes.MSBuild;
using NuGetNodes = System.Trees.Nodes.NuGet;
using TreeNodes = System.Trees.Nodes;
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

		var src = root.Children["src"].Should().BeOfType<FileSystemNodes.FolderTreeNode>().Subject;
		src.TotalLines.Should().Be(3);
		src.TotalLabelCount.Should().Be(1);

		var appNode = src.Children["App.cs"].Should().BeOfType<FileSystemNodes.FileTreeNode>().Subject;
		appNode.File.FullName.Should().Be(app.FullName);
		appNode.FullPath.Should().Be(app.FullName);
		appNode.RelativePath.Should().Be("src/App.cs");
		appNode.LineCount.Should().Be(2);
		appNode.LabelCount.Should().Be(1);

		var readmeNode = src.Children["Readme.md"].Should().BeOfType<FileSystemNodes.FileTreeNode>().Subject;
		readmeNode.RelativePath.Should().Be("src/Readme.md");
		readmeNode.LineCount.Should().Be(1);
		readmeNode.LabelCount.Should().Be(0);
	}

	[Fact]
	public void Parse_KnownFileTypes_UsesSpecializedNodes() {
		using var temp = new TestDirectory();
		var solution = temp.CreateFile("HawsLabs.Extensions.slnx");
		var project = temp.CreateFile("src/App/App.csproj");
		var package = temp.CreateFile("artifacts/HawsLabs.Extensions.System.1.2.3-beta.1.nupkg");
		var zip = temp.CreateFile("artifacts/build.zip");

		var root = FileTreeParser.Parse(
			[solution, project, package, zip],
			new() {
				BasePath = temp.Root.FullName,
			}
		);

		root.Children["HawsLabs.Extensions.slnx"]
			.Should().BeOfType<MSBuildNodes.SolutionNode>()
			.Which.RelativePath.Should().Be("HawsLabs.Extensions.slnx");

		var app = root.Children["src"]
			.Should().BeOfType<FileSystemNodes.FolderTreeNode>()
			.Subject.Children["App"].Should().BeOfType<FileSystemNodes.FolderTreeNode>().Subject;

		app.Children["App.csproj"]
			.Should().BeOfType<MSBuildNodes.ProjectNode>()
			.Which.File.FullName.Should().Be(project.FullName);

		var artifacts = root.Children["artifacts"].Should().BeOfType<FileSystemNodes.FolderTreeNode>().Subject;

		var packageNode = artifacts.Children["HawsLabs.Extensions.System.1.2.3-beta.1.nupkg"]
			.Should().BeOfType<NuGetNodes.NuGetPackageNode>().Subject;

		packageNode.File.FullName.Should().Be(package.FullName);
		packageNode.PackageId.Should().Be("HawsLabs.Extensions.System");
		packageNode.PackageVersion.Should().Be("1.2.3-beta.1");

		artifacts.Children["build.zip"]
			.Should().BeOfType<ArchiveNodes.ZipArchiveNode>()
			.Which.File.FullName.Should().Be(zip.FullName);
	}

	[Fact]
	public void Parse_NodeParsersHaveSameSpecificityAndPriority_ThrowsAmbiguousParserMatch() {
		using var temp = new TestDirectory();
		var file = temp.CreateFile("src/App.txt");

		var act = () => FileTreeParser.Parse(
			[file],
			new() {
				BasePath = temp.Root.FullName,
				NodeParsers = [
					new TestTreeNodeParser("first parser", specificity: 100, priority: 0),
					new TestTreeNodeParser("second parser", specificity: 100, priority: 0),
				],
			}
		);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Ambiguous parser match*first parser*second parser*");
	}

	[Fact]
	public void Parse_NodeParsersHaveSameSpecificityAndDifferentPriority_UsesHighestPriorityParser() {
		using var temp = new TestDirectory();
		var file = temp.CreateFile("src/App.txt");

		var root = FileTreeParser.Parse(
			[file],
			new() {
				BasePath = temp.Root.FullName,
				NodeParsers = [
					new TestTreeNodeParser("low priority parser", specificity: 100, priority: 0, nodeName: "low"),
					new TestTreeNodeParser("high priority parser", specificity: 100, priority: 10, nodeName: "high"),
				],
			}
		);

		var src = root.Children["src"].Should().BeOfType<FileSystemNodes.FolderTreeNode>().Subject;
		src.Children["App.txt"].Name.Should().Be("high");
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

		var src = root.Children["src"].Should().BeOfType<FileSystemNodes.FolderTreeNode>().Subject;
		var app = src.Children["App.cs"].Should().BeOfType<FileSystemNodes.FileTreeNode>().Subject;

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

		var src = root.Children["src"].Should().BeOfType<FileSystemNodes.FolderTreeNode>().Subject;

		src.Children.Values.Should().ContainSingle()
			.Which.Should().BeOfType<FileSystemNodes.FileTreeNode>()
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

		var src = root.Children["src"].Should().BeOfType<FileSystemNodes.FolderTreeNode>().Subject;
		var app = src.Children["App.cs"].Should().BeOfType<FileSystemNodes.FileTreeNode>().Subject;

		app.LineCount.Should().Be(0);
		root.TotalLines.Should().Be(0);
	}

	[Fact]
	public void GetOrAddFolder_ChildNameDiffersOnlyByCase_ReturnsExistingFolder() {
		var root = new FileSystemNodes.FolderTreeNode("root");
		var folder = root.GetOrAddFolder("src");

		var sameFolder = root.GetOrAddFolder("SRC");

		sameFolder.Should().BeSameAs(folder);
		root.Children.Values.Should().ContainSingle();
	}

	private sealed class TestTreeNodeParser(
		string reason,
		int specificity,
		int priority,
		string? nodeName = null
	) : ITreeNodeParser {
		public int Priority { get; } = priority;

		public TreeNodeParserMatch Match(TreeNodeParseContext context) {
			return new(true, specificity, reason);
		}

		public TreeNodes.TreeNode Parse(TreeNodeParseContext context) {
			return TreeNodes.TreeNode.Parse(nodeName ?? context.File.Name);
		}
	}
}
