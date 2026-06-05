namespace System.Trees.MSBuild.Nodes;

using System.Trees;

public sealed record SolutionNode(
	FileInfo File,
	string FullPath,
	string RelativePath
) : TreeNode(File.Name) {
	public override string? Icon { get; init; } = "🧩";

	public static SolutionNode Parse(FileInfo file, string relativePath) {
		ArgumentNullException.ThrowIfNull(file);
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		return new(file, file.FullName, relativePath);
	}
}
