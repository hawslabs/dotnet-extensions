namespace HawsLabs.Extensions.Tests.IO;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class FileInfoEnumerableExtensionsTests {
	[Fact]
	public void FindCommonBasePath_FilesCollectionIsEmpty_ReturnsCurrentDirectory() {
		var files = Array.Empty<FileInfo>();

		files.FindCommonBasePath(StringComparison.Ordinal).Should().Be(Directory.GetCurrentDirectory());
	}

	[Fact]
	public void FindCommonBasePath_FilesShareParentDirectory_ReturnsParentDirectory() {
		using var temp = new TestDirectory();
		var parent = temp.CreateDirectory("src");
		var files = new[] {
			temp.CreateFile("src/First.cs"),
			temp.CreateFile("src/Second.cs"),
		};

		files.FindCommonBasePath(StringComparison.Ordinal).Should().Be(parent.FullName);
	}

	[Fact]
	public void FindCommonBasePath_FilesAreInSiblingDirectories_ReturnsSharedAncestorDirectory() {
		using var temp = new TestDirectory();
		var files = new[] {
			temp.CreateFile("src/First.cs"),
			temp.CreateFile("tests/Second.cs"),
		};

		files.FindCommonBasePath(StringComparison.Ordinal).Should().Be(temp.Root.FullName);
	}
}