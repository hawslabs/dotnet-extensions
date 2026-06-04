namespace HawsLabs.Extensions.Tests.Trees;

using System.Trees.Formatting;
using System.Trees.Nodes;
using FluentAssertions;
using Xunit;

public sealed class TreeNodeTests {
	[Fact]
	public void GetOrAdd_ChildDoesNotExist_AddsGenericChild() {
		var root = new TreeNode("root");

		var child = root.GetOrAdd("item");

		child.Should().BeOfType<TreeNode>();
		child.Name.Should().Be("item");
		root.Children.Values.Should().ContainSingle().Which.Should().BeSameAs(child);
	}

	[Fact]
	public void GetOrAdd_ChildNameDiffersOnlyByCase_ReturnsExistingChild() {
		var root = new TreeNode("root");
		var child = root.GetOrAdd("src");

		var sameChild = root.GetOrAdd("SRC");

		sameChild.Should().BeSameAs(child);
		root.Children.Values.Should().ContainSingle();
	}

	[Fact]
	public void Format_FormatterIsProvided_DelegatesToFormatter() {
		var root = new TreeNode("root");
		var formatter = new CapturingTreeNodeFormatter();

		var formatted = root.Format(formatter);

		formatted.Should().Be("root");
		formatter.Root.Should().BeSameAs(root);
	}

	private sealed class CapturingTreeNodeFormatter : ITreeNodeFormatter {
		public TreeNode? Root { get; private set; }

		public string Format(TreeNode root) {
			Root = root;

			return root.Name;
		}
	}
}