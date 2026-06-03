using System.Trees.Formatting.Ascii;

public static class DumpAsAsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public string DumpAsAsciiTree(AsciiTreeFormatterOptions? options = null) {
			var tree = paths.ToAsciiTree(options);

			Util.RawHtml(
				$"""
				<pre style='font-family:Consolas,monospace;font-size:13px'>{System.Net.WebUtility.HtmlEncode(tree)}</pre>
				"""
			).Dump();

			return tree;
		}
	}
}