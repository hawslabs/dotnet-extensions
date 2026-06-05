namespace System.Trees.FileSystem.Parsing;

public readonly record struct TreeNodeParserMatch(
	bool IsMatch,
	int Specificity,
	string? Reason = null
) {
	public static TreeNodeParserMatch None { get; } = new(false, 0);

	public static TreeNodeParserMatch Exact(string? reason = null) {
		return new(true, TreeNodeParserSpecificity.ExactFormat, reason);
	}

	public static TreeNodeParserMatch Strong(string? reason = null) {
		return new(true, TreeNodeParserSpecificity.ContentShape, reason);
	}

	public static TreeNodeParserMatch Normal(string? reason = null) {
		return new(true, TreeNodeParserSpecificity.FileSignature, reason);
	}

	public static TreeNodeParserMatch Weak(string? reason = null) {
		return new(true, TreeNodeParserSpecificity.Extension, reason);
	}

	public static TreeNodeParserMatch Fallback(string? reason = null) {
		return new(true, TreeNodeParserSpecificity.Fallback, reason);
	}
}
