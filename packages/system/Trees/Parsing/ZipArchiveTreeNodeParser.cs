namespace System.Trees.Parsing;

using System.Trees;
using ArchiveNodes = System.Trees.Archive.Nodes;

public sealed class ZipArchiveTreeNodeParser : ITreeNodeParser {
	public TreeNodeParserMatch Match(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		if (!context.File.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
			return TreeNodeParserMatch.None;
		}

		return new(
			IsMatch: true,
			Specificity: TreeNodeParserSpecificity.Extension,
			Reason: "File has .zip extension."
		);
	}

	public TreeNode Parse(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		return ArchiveNodes.ZipArchiveNode.Parse(context.File, context.RelativePath);
	}
}
