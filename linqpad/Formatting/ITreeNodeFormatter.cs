using HawsLabs.Extensions.LINQPad.Trees;

namespace HawsLabs.Extensions.LINQPad.Formatting;

public interface ITreeNodeFormatter {
    string Format(TreeNode root);
}
