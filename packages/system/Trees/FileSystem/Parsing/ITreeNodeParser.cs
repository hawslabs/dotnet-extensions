namespace System.Trees.FileSystem.Parsing;

using System.Trees;

public interface ITreeNodeParser {
	int Priority => 0;

	TreeNodeParserMatch Match(TreeNodeParseContext context);

	TreeNode Parse(TreeNodeParseContext context);
}
