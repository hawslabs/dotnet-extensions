namespace System.Trees.Formatting.Ascii;

/// <summary>
/// Represents a single cell in an ASCII grid.
/// </summary>
/// <param name="Text">The text content to render inside the cell.</param>
public sealed record AsciiCell(string Text) {
	public int Row { get; init; }
	public int Column { get; init; }
	public int RowSpan { get; init; } = 1;
	public int ColumnSpan { get; init; } = 1;
	public AsciiHorizontalAlignment HorizontalAlignment { get; init; } = AsciiHorizontalAlignment.Left;
	public AsciiVerticalAlignment VerticalAlignment { get; init; } = AsciiVerticalAlignment.Top;
	public int PaddingLeft { get; init; }
	public int PaddingRight { get; init; }
	public int PaddingTop { get; init; }
	public int PaddingBottom { get; init; }
}