namespace HawsLabs.Extensions.Tests.Collections.Generic;

using FluentAssertions;

using Xunit;

public sealed class EnumerableAncestorExtensionsTests {
	[Fact]
	public void FindCommonAncestor_SourceIsEmpty_ReturnsNull() {
		var nodes = Array.Empty<TestNode>();

		nodes.FindCommonAncestor(static node => node.Parent).Should().BeNull();
	}

	[Fact]
	public void FindCommonAncestor_SourceContainsSiblings_ReturnsSharedParent() {
		var root = new TestNode("root");
		var parent = new TestNode("parent", root);
		var left = new TestNode("left", parent);
		var right = new TestNode("right", parent);

		var ancestor = new[] { left, right }.FindCommonAncestor(static node => node.Parent);

		ancestor.Should().BeSameAs(parent);
	}

	[Fact]
	public void FindCommonAncestor_SourceContainsDifferentTrees_ReturnsNull() {
		var left = new TestNode("left");
		var right = new TestNode("right");

		var ancestor = new[] { left, right }.FindCommonAncestor(static node => node.Parent);

		ancestor.Should().BeNull();
	}

	private sealed class TestNode(string name, TestNode? parent = null) {
		public string Name { get; } = name;

		public TestNode? Parent { get; } = parent;
	}
}
