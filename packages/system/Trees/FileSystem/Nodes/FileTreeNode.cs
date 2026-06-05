namespace System.Trees.FileSystem.Nodes;

using System.Trees;
using System.Trees.Parsing;

public sealed record FileTreeNode(
	FileInfo File,
	string FullPath,
	string RelativePath,
	int LineCount = 0,
	int LabelCount = 0
) : TreeNode(File.Name) {
	public override string? Icon { get; init; } = "📄";

	public static FileTreeNode Parse(FileInfo file, string relativePath, FileTreeParseOptions options) {
		ArgumentNullException.ThrowIfNull(file);
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
		ArgumentNullException.ThrowIfNull(options);

		if (!options.ReadFileMetrics) {
			return new(file, file.FullName, relativePath);
		}

		try {
			var lineCount = 0;
			var labelCount = 0;

			foreach (var line in options.ReadLines(file.FullName)) {
				lineCount++;

				if (options.LabelPredicate(line)) {
					labelCount++;
				}
			}

			return new(file, file.FullName, relativePath, lineCount, labelCount);
		} catch when (options.IgnoreFileReadErrors) {
			return new(file, file.FullName, relativePath);
		}
	}
}
