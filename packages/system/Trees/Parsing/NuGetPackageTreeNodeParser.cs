namespace System.Trees.Parsing;

using System.Trees;
using NuGetNodes = System.Trees.NuGet.Nodes;

public sealed class NuGetPackageTreeNodeParser : ITreeNodeParser {
	private static readonly HashSet<string> PackageExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".nupkg",
		".snupkg",
	};

	public TreeNodeParserMatch Match(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		if (!PackageExtensions.Contains(context.File.Extension)) {
			return TreeNodeParserMatch.None;
		}

		return TreeNodeParserMatch.Exact("File has known NuGet package extension.");
	}

	public TreeNode Parse(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		return NuGetNodes.NuGetPackageNode.Parse(context.File, context.RelativePath);
	}
}
