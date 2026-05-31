namespace HawsLabs.Extensions.LINQPad.Formatting.AsciiTree;

using HawsLabs.Extensions.LINQPad.Trees;

public static class AsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public string ToAsciiTree(string basePath, AsciiTreeNodeFormatterOptions? options = null) {
			options ??= new AsciiTreeNodeFormatterOptions {
				SortOrder = TreeSortOrder.Alphabetical,
				ShowIcons = true,
				ShowLabels = true,
				AlignColumns = true,
			};

			var rootNode = TreeNode.Parse(
				paths,
				new TreeNodeParseOptions {
					BasePath = basePath,
					LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
				}
			);

			return rootNode.Format(
				new AsciiTreeNodeFormatter(options)
			);
		}

		public object DumpAsAsciiTree(string basePath, AsciiTreeNodeFormatterOptions? options = null) {
			var tree = paths.ToAsciiTree(basePath, options);
			return Util.RawHtml(
				$"""
				<pre style='font-family:Consolas,monospace;font-size:13px'>{tree}</pre>
				"""
			).Dump();
		}
	}
}