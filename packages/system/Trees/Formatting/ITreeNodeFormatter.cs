namespace System.Trees.Formatting;

using Nodes;

public interface ITreeNodeFormatter {
	string Format(TreeNode root);
}