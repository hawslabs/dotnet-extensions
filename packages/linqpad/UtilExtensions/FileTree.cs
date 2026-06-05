using System.Trees.FileSystem;
using System.Trees.FileSystem.Parsing;
using JetBrains.Annotations;

[UsedImplicitly(Reason = "Used by LINQPad sample queries.")]
public static class FileTreeUtils {
	extension(Util) {
		[UsedImplicitly(Reason = "Used by LINQPad sample queries.")]
		public static FileTree GetFileTree(
			DirectoryInfo rootDirectory,
			GlobsOptions? options = null
		) {
			return rootDirectory.Glob(options).ToFileTree();
		}
	}
}
