namespace System.Trees.Parsing;

using System.Trees;
using FileSystemNodes = System.Trees.FileSystem.Nodes;

public sealed class DefaultTreeNodeParser : ITreeNodeParser {
	public TreeNodeParserMatch Match(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		return TreeNodeParserMatch.Fallback("Default file parser.");
	}

	public TreeNode Parse(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		return FileSystemNodes.FileTreeNode.Parse(
			context.File,
			context.RelativePath,
			context.Options
		);
	}
}
