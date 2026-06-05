namespace System.Trees.Parsing;

public static class TreeNodeParsers {
	private static readonly ITreeNodeParser[] defaultParsers = [
		new NuGetPackageTreeNodeParser(),
		new ZipArchiveTreeNodeParser(),
		new SolutionTreeNodeParser(),
		new ProjectTreeNodeParser(),
		new DefaultTreeNodeParser(),
	];

	public static IReadOnlyList<ITreeNodeParser> Default => defaultParsers;
}
