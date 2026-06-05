namespace System.Trees.Parsing;

using System.Trees.Nodes;
using MSBuildNodes = System.Trees.Nodes.MSBuild;

public sealed class SolutionTreeNodeParser : ITreeNodeParser {
	private static readonly HashSet<string> SolutionExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".sln",
		".slnx",
	};

	public TreeNodeParserMatch Match(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		if (!SolutionExtensions.Contains(context.File.Extension)) {
			return TreeNodeParserMatch.None;
		}

		return TreeNodeParserMatch.Exact("File has known MSBuild solution extension.");
	}

	public TreeNode Parse(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		return MSBuildNodes.SolutionNode.Parse(context.File, context.RelativePath);
	}
}
