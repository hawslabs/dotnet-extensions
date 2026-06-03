using System.Trees.Formatting.Ascii;

public static class AsciiTreeExtensions {
	extension(AsciiTree tree) {
		public AsciiTree Dump() {
			ArgumentNullException.ThrowIfNull(tree);

			Util.RawHtml(
				$"""
				<pre style='font-family:Consolas,monospace;font-size:13px'>{System.Net.WebUtility.HtmlEncode(tree.Text)}</pre>
				"""
			).Dump();

			return tree;
		}
	}
}