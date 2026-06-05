namespace System.Trees.Nodes;

using Formatting;

public record TreeNode(string Name) {
	public OrderedDictionary<string, TreeNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

	public static TreeNode Parse(string name) {
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		return new(name);
	}

	public string Format(ITreeNodeFormatter formatter) {
		ArgumentNullException.ThrowIfNull(formatter);
		return formatter.Format(this);
	}

	public TreeNode GetOrAdd(string name) {
		if (Children.TryGetValue(name, out var child)) {
			return child;
		}

		child = Parse(name);
		Children[name] = child;

		return child;
	}
}
