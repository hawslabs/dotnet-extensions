namespace HawsLabs.Extensions.Tests.IO;

using FluentAssertions;

using HawsLabs.Extensions.Tests;

using Xunit;

public sealed class ParentDirectoryExtensionsTests {
	[Fact]
	public void GetParentDirectory_DefaultLevel_ReturnsImmediateParent() {
		using var temp = new TestDirectory();
		var parent = temp.CreateDirectory("one");
		var child = temp.CreateDirectory("one/two");

		child.GetParentDirectory().FullName.Should().Be(parent.FullName);
	}

	[Fact]
	public void GetParentDirectory_MultipleLevels_ReturnsAncestor() {
		using var temp = new TestDirectory();
		var ancestor = temp.CreateDirectory("one");
		var child = temp.CreateDirectory("one/two/three");

		child.GetParentDirectory(2).FullName.Should().Be(ancestor.FullName);
	}

	[Fact]
	public void GetParentDirectory_LevelBeyondRoot_ThrowsDirectoryNotFoundException() {
		using var temp = new TestDirectory();
		var child = temp.CreateDirectory("one");

		var act = () => child.GetParentDirectory(1000);

		act.Should().Throw<DirectoryNotFoundException>();
	}

	[Fact]
	public void GetParentDirectoryOrRoot_LevelBeyondRoot_ReturnsRootDirectory() {
		using var temp = new TestDirectory();
		var child = temp.CreateDirectory("one");

		child.GetParentDirectoryOrRoot(1000).FullName.Should().Be(child.Root.FullName);
	}

	[Fact]
	public void TryGetParentDirectory_ZeroLevels_ReturnsOriginalDirectory() {
		using var temp = new TestDirectory();
		var directory = temp.CreateDirectory("one");

		var result = directory.TryGetParentDirectory(0, out var parentDirectory);

		result.Should().BeTrue();
		parentDirectory.Should().NotBeNull();
		parentDirectory.FullName.Should().Be(directory.FullName);
	}

	[Fact]
	public void TryGetParentDirectory_LevelBeyondRoot_ReturnsFalseAndNullParentDirectory() {
		using var temp = new TestDirectory();
		var directory = temp.CreateDirectory("one");

		var result = directory.TryGetParentDirectory(1000, out var parentDirectory);

		result.Should().BeFalse();
		parentDirectory.Should().BeNull();
	}
}