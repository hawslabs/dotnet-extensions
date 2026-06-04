namespace HawsLabs.Extensions.Tests.Formatting.Ascii;

using System.Trees.Formatting.Ascii;

using FluentAssertions;

using Xunit;

public sealed class AsciiGridTests {
	[Fact]
	public void Render_CellSpansColumnsAndRows_RendersAcrossTheSpan() {
		var grid = new AsciiGrid {
			ColumnSpacing = 0,
		};

		grid.AddCell(new("AB") {
			Row = 0,
			Column = 0,
			RowSpan = 2,
			ColumnSpan = 2,
		});
		grid.AddCell(new("C") {
			Row = 0,
			Column = 2,
		});
		grid.AddCell(new("D") {
			Row = 1,
			Column = 2,
		});

		grid.Render().Should().Be(
			string.Join(
				Environment.NewLine,
				"ABC",
				"  D",
				""
			)
		);
	}

	[Fact]
	public void Render_CellCanVerticallyAlignWithinASpan_RendersMiddleAlignedContent() {
		var grid = new AsciiGrid {
			ColumnSpacing = 0,
		};

		grid.AddCell(new("X") {
			Row = 0,
			Column = 0,
			RowSpan = 3,
			VerticalAlignment = AsciiVerticalAlignment.Middle,
		});
		grid.AddCell(new("A") {
			Row = 0,
			Column = 1,
		});
		grid.AddCell(new("B") {
			Row = 1,
			Column = 1,
		});
		grid.AddCell(new("C") {
			Row = 2,
			Column = 1,
		});

		grid.Render().Should().Be(
			string.Join(
				Environment.NewLine,
				" A",
				"XB",
				" C",
				""
			)
		);
	}
}