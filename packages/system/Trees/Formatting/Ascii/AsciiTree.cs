namespace System.Trees.Formatting.Ascii;

using JetBrains.Annotations;
using Xml.Linq;

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

    [UsedImplicitly(Reason = "Used by LINQPad rendering adapter.")]
    private XElement ToDump() {
	    return new(
		    name: "LINQPad.HTML",
		    content: new XElement(
			    "pre",
			    new XAttribute("style", "font-family:Consolas,monospace;font-size:13px"),
			    Text
		    )
	    );
    }
}