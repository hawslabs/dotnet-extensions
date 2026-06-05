namespace System.Trees.Parsing;

using System.Trees.Nodes;

public interface ITreeNodeParser {
	int Priority => 0;

	TreeNodeParserMatch Match(TreeNodeParseContext context);

	TreeNode Parse(TreeNodeParseContext context);
}
