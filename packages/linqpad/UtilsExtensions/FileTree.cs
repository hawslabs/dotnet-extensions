using System.Trees.Formatting.Ascii;
using System.Trees.Parsing;
using JetBrains.Annotations;

[UsedImplicitly(Reason = "Used by LINQPad sample queries.")]
public static class FileTreeUtils {
	extension(Util) {
		[UsedImplicitly(Reason = "Used by LINQPad sample queries.")]
		public static FileTree GetFileTree(
			DirectoryInfo rootDirectory,
			IEnumerable<string>? includePatterns = null,
			IEnumerable<string>? excludePatterns = null,
			GlobsOptions? options = null
		) {
			return rootDirectory.Glob(
				includePatterns: includePatterns,
				excludePatterns: excludePatterns,
				options: options
			).ToFileTree();
		}
	}
}