namespace HawsLabs.Extensions.Tests.IO;

using FluentAssertions;

using Xunit;

public sealed class PathExtensionsTests {
	[Theory]
	[InlineData("..")]
	[InlineData("../sibling")]
	[InlineData("..\\sibling")]
	[InlineData("child/../..")]
	[InlineData("child\\..\\..")]
	public void IsOutsideBasePath_RelativePathEscapesBasePath_ReturnsTrue(string path) {
		var basePath = Path.Combine(Path.GetTempPath(), "base", "child");

		Path.IsOutsideBasePath(basePath, path, StringComparison.Ordinal).Should().BeTrue();
	}

	[Theory]
	[InlineData("")]
	[InlineData(".")]
	[InlineData("./child")]
	[InlineData("child")]
	[InlineData("child/..")]
	[InlineData("child/../sibling")]
	[InlineData("child\\..")]
	[InlineData("child\\..\\sibling")]
	public void IsOutsideBasePath_RelativePathStaysInsideBasePath_ReturnsFalse(string path) {
		var basePath = Path.Combine(Path.GetTempPath(), "base", "child");

		Path.IsOutsideBasePath(basePath, path, StringComparison.Ordinal).Should().BeFalse();
	}

	[Fact]
	public void IsOutsideBasePath_RootedPathIsInsideBasePath_ReturnsFalse() {
		var basePath = Path.Combine(Path.GetTempPath(), "foo", "bar");
		var path = Path.Combine(basePath, "maz");

		Path.IsOutsideBasePath(basePath, path, StringComparison.Ordinal).Should().BeFalse();
	}

	[Fact]
	public void IsOutsideBasePath_RootedPathIsOutsideBasePath_ReturnsTrue() {
		var basePath = Path.Combine(Path.GetTempPath(), "foo", "bar");
		var path = Path.Combine(Path.GetTempPath(), "foo", "baz");

		Path.IsOutsideBasePath(basePath, path, StringComparison.Ordinal).Should().BeTrue();
	}

	[Fact]
	public void IsSameOrChildPath_PathsAreSame_ReturnsTrue() {
		var path = Path.Combine(Path.GetTempPath(), "parent") + Path.DirectorySeparatorChar;
		var candidateParent = Path.Combine(Path.GetTempPath(), "parent");

		Path.IsSameOrChildPath(path, candidateParent, StringComparison.Ordinal).Should().BeTrue();
	}

	[Fact]
	public void IsSameOrChildPath_PathIsChild_ReturnsTrue() {
		var candidateParent = Path.Combine(Path.GetTempPath(), "parent");
		var path = Path.Combine(candidateParent, "child", "file.txt");

		Path.IsSameOrChildPath(path, candidateParent, StringComparison.Ordinal).Should().BeTrue();
	}

	[Fact]
	public void IsSameOrChildPath_PathOnlySharesPrefix_ReturnsFalse() {
		var candidateParent = Path.Combine(Path.GetTempPath(), "parent");
		var path = candidateParent + "-sibling";

		Path.IsSameOrChildPath(path, candidateParent, StringComparison.Ordinal).Should().BeFalse();
	}

	[Fact]
	public void IsSameOrChildPath_ComparisonDoesNotMatchCase_ReturnsFalse() {
		var candidateParent = Path.Combine(Path.GetTempPath(), "parent");
		var path = Path.Combine(Path.GetTempPath(), "PARENT", "child");

		Path.IsSameOrChildPath(path, candidateParent, StringComparison.Ordinal).Should().BeFalse();
	}

	[Fact]
	public void IsSameOrChildPath_ComparisonIgnoresCase_ReturnsTrue() {
		var candidateParent = Path.Combine(Path.GetTempPath(), "parent");
		var path = Path.Combine(Path.GetTempPath(), "PARENT", "child");

		Path.IsSameOrChildPath(path, candidateParent, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
	}

	[Fact]
	public void GetFileNameOrPath_PathHasFileName_ReturnsFileName() {
		var path = Path.Combine(Path.GetTempPath(), "parent", "file.txt");

		Path.GetFileNameOrPath(path).Should().Be("file.txt");
	}

	[Fact]
	public void GetFileNameOrPath_PathHasTrailingSeparator_ReturnsLastDirectoryName() {
		var path = Path.Combine(Path.GetTempPath(), "parent") + Path.DirectorySeparatorChar;

		Path.GetFileNameOrPath(path).Should().Be("parent");
	}
}