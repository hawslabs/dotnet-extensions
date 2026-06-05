namespace System.Trees.FileSystem.Parsing;

using System.Trees.Archive.Parsing;
using System.Trees.MSBuild.Parsing;
using System.Trees.NuGet.Parsing;

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
