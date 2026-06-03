using System.Trees.Formatting.Ascii;

public static class DumpAsAsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public object DumpAsAsciiTree(string basePath, AsciiTreeFormatterOptions? options = null) {
			var tree = System.Net.WebUtility.HtmlEncode(paths.ToAsciiTree(basePath, options));
			return Util.RawHtml(
				$"""
				<pre style='font-family:Consolas,monospace;font-size:13px'>{tree}</pre>
				"""
			).Dump();
		}
	}
}