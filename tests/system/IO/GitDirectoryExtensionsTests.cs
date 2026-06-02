namespace HawsLabs.Extensions.Tests.IO;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class GitDirectoryExtensionsTests {
	[Fact]
	public void IsGitRootDirectory_DirectoryContainsDotGitDirectory_ReturnsTrue() {
		using var temp = new TestDirectory();
		temp.CreateDirectory(".git");

		temp.Root.IsGitRootDirectory.Should().BeTrue();
	}

	[Fact]
	public void IsGitRootDirectory_DirectoryDoesNotExist_ReturnsFalse() {
		using var temp = new TestDirectory();
		var missingDirectory = new DirectoryInfo(Path.Combine(temp.Root.FullName, "missing"));

		missingDirectory.IsGitRootDirectory.Should().BeFalse();
	}

	[Fact]
	public void GitRootDirectory_AncestorContainsDotGitDirectory_ReturnsAncestor() {
		using var temp = new TestDirectory();
		temp.CreateDirectory(".git");
		var nested = temp.CreateDirectory("src/feature");

		nested.GitRootDirectory.Should().NotBeNull();
		nested.GitRootDirectory!.FullName.Should().Be(temp.Root.FullName);
	}

	[Fact]
	public void GitRootDirectory_NoAncestorContainsDotGitDirectory_ReturnsNull() {
		using var temp = new TestDirectory();
		var nested = temp.CreateDirectory("src/feature");

		nested.GitRootDirectory.Should().BeNull();
	}
}