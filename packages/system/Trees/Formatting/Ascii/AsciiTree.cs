namespace System.Trees.Formatting.Ascii;

/// <summary>
/// Represents formatted ASCII tree output and the root path used to build it.
/// </summary>
/// <param name="RootPath">The full root path used when parsing the tree.</param>
/// <param name="Text">The formatted ASCII tree text.</param>
public sealed record AsciiTree(
	string RootPath,
	string Text
) {
	/// <inheritdoc />
	public override string ToString() {
		return Text;
	}
}