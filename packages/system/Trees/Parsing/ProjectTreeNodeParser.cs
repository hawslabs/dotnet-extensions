namespace System.Trees.Parsing;

using System.Trees;
using MSBuildNodes = System.Trees.MSBuild.Nodes;

public sealed class ProjectTreeNodeParser : ITreeNodeParser {
	private static readonly HashSet<string> ProjectExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".csproj",
		".fsproj",
		".props",
		".targets",
		".vbproj",
	};

	public TreeNodeParserMatch Match(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		if (!ProjectExtensions.Contains(context.File.Extension)) {
			return TreeNodeParserMatch.None;
		}

		return TreeNodeParserMatch.Exact("File has known MSBuild project extension.");
	}

	public TreeNode Parse(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		return MSBuildNodes.ProjectNode.Parse(context.File, context.RelativePath);
	}
}
