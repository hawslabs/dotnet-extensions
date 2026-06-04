namespace System.Trees.Formatting.Ascii;

/// <summary>
/// Renders a span-aware ASCII grid with per-cell alignment and padding.
/// </summary>
public sealed class AsciiGrid {
	private readonly List<AsciiCell> cells = [];

	public IReadOnlyList<AsciiCell> Cells => cells;

	public int ColumnSpacing { get; init; } = 1;

	public AsciiGrid AddCell(AsciiCell cell) {
		ArgumentNullException.ThrowIfNull(cell);

		cells.Add(cell);

		return this;
	}

	public string Render() {
		if (cells.Count == 0) {
			return string.Empty;
		}

		var placements = cells
			.Select((cell, index) => new CellPlacement(index, cell))
			.OrderBy(placement => placement.Cell.Row)
			.ThenBy(placement => placement.Cell.Column)
			.ThenBy(placement => placement.Index)
			.ToList();

		ValidatePlacements(placements);

		var rowCount = placements.Max(placement => placement.Cell.Row + placement.Cell.RowSpan);
		var columnCount = placements.Max(placement => placement.Cell.Column + placement.Cell.ColumnSpan);
		var columnWidths = new int[columnCount];
		var rowHeights = new int[rowCount];

		MeasureColumns(placements, columnWidths, ColumnSpacing);
		MeasureRows(placements, rowHeights);

		var rowOffsets = new int[rowCount + 1];

		for (var row = 0; row < rowCount; row++) {
			rowOffsets[row + 1] = rowOffsets[row] + rowHeights[row];
		}

		var renderedCells = new string[placements.Count][];
		var rowPlacements = new List<int>[rowCount];

		for (var row = 0; row < rowCount; row++) {
			rowPlacements[row] = [];
		}

		for (var i = 0; i < placements.Count; i++) {
			var placement = placements[i];
			var cell = placement.Cell;
			var cellWidth = GetSpanLength(columnWidths, cell.Column, cell.ColumnSpan, ColumnSpacing);
			var cellHeight = GetSpanLength(rowHeights, cell.Row, cell.RowSpan, 0);

			renderedCells[i] = RenderCell(cell, cellWidth, cellHeight);

			for (var row = cell.Row; row < cell.Row + cell.RowSpan; row++) {
				rowPlacements[row].Add(i);
			}
		}

		for (var row = 0; row < rowPlacements.Length; row++) {
			rowPlacements[row].Sort((left, right) => placements[left].Cell.Column.CompareTo(placements[right].Cell.Column));
		}

		var sb = new StringBuilder();

		for (var row = 0; row < rowCount; row++) {
			var rowHeight = rowHeights[row];

			for (var lineInRow = 0; lineInRow < rowHeight; lineInRow++) {
				sb.AppendLine(RenderRowLine(row, lineInRow, columnWidths, rowOffsets, placements, renderedCells, rowPlacements));
			}
		}

		return sb.ToString();
	}

	private string RenderRowLine(
		int row,
		int lineInRow,
		int[] columnWidths,
		int[] rowOffsets,
		IReadOnlyList<CellPlacement> placements,
		IReadOnlyList<string[]> renderedCells,
		List<int>[] rowPlacements
	) {
		var sb = new StringBuilder();
		var activePlacementIndex = 0;

		for (var column = 0; column < columnWidths.Length; column++) {
			while (activePlacementIndex < rowPlacements[row].Count
				&& placements[rowPlacements[row][activePlacementIndex]].Cell.Column < column) {
				activePlacementIndex++;
			}

			if (activePlacementIndex >= rowPlacements[row].Count
				|| placements[rowPlacements[row][activePlacementIndex]].Cell.Column != column) {
				sb.Append(' ', columnWidths[column]);
			} else {
				var placementIndex = rowPlacements[row][activePlacementIndex];
				var placement = placements[placementIndex];
				var cell = placement.Cell;
				var lineIndex = rowOffsets[row] - rowOffsets[cell.Row] + lineInRow;

				sb.Append(renderedCells[placementIndex][lineIndex]);
				column += cell.ColumnSpan - 1;
				activePlacementIndex++;
			}

			if (column < columnWidths.Length - 1 && ColumnSpacing > 0) {
				sb.Append(' ', ColumnSpacing);
			}
		}

		return sb.ToString();
	}

	private static string[] RenderCell(AsciiCell cell, int width, int height) {
		var lines = SplitLines(cell.Text);
		var contentWidth = lines.Max(line => line.Length);
		var contentHeight = lines.Count;
		var innerWidth = width - cell.PaddingLeft - cell.PaddingRight;
		var innerHeight = height - cell.PaddingTop - cell.PaddingBottom;

		if (innerWidth < 0 || innerHeight < 0) {
			throw new InvalidOperationException("Cell padding exceeds the available grid space.");
		}

		if (contentWidth > innerWidth) {
			throw new InvalidOperationException("Cell content exceeds the measured grid width.");
		}

		if (contentHeight > innerHeight) {
			throw new InvalidOperationException("Cell content exceeds the measured grid height.");
		}

		var alignedLines = lines
			.Select(line => AlignHorizontal(line, innerWidth, cell.HorizontalAlignment))
			.ToArray();
		var topPadding = cell.VerticalAlignment switch {
			AsciiVerticalAlignment.Top => 0,
			AsciiVerticalAlignment.Middle => (innerHeight - contentHeight) / 2,
			AsciiVerticalAlignment.Bottom => innerHeight - contentHeight,
			_ => 0,
		};
		var result = new string[height];
		var blankLine = new string(' ', width);

		for (var i = 0; i < height; i++) {
			if (i < cell.PaddingTop || i >= height - cell.PaddingBottom) {
				result[i] = blankLine;
				continue;
			}

			var contentLineIndex = i - cell.PaddingTop - topPadding;

			if (contentLineIndex < 0 || contentLineIndex >= alignedLines.Length) {
				result[i] = blankLine;
				continue;
			}

			result[i] = new string(' ', cell.PaddingLeft)
				+ alignedLines[contentLineIndex]
				+ new string(' ', cell.PaddingRight);
		}

		return result;
	}

	private static string AlignHorizontal(string text, int width, AsciiHorizontalAlignment alignment) {
		if (text.Length >= width) {
			return text;
		}

		var padding = width - text.Length;

		return alignment switch {
			AsciiHorizontalAlignment.Left => text.PadRight(width),
			AsciiHorizontalAlignment.Center => new string(' ', padding / 2) + text + new string(' ', padding - padding / 2),
			AsciiHorizontalAlignment.Right => text.PadLeft(width),
			_ => text,
		};
	}

	private static IReadOnlyList<string> SplitLines(string text) {
		if (string.IsNullOrEmpty(text)) {
			return [string.Empty];
		}

		return text
			.Replace("\r\n", "\n")
			.Replace('\r', '\n')
			.Split('\n');
	}

	private static void ValidatePlacements(IReadOnlyList<CellPlacement> placements) {
		if (placements.Count == 0) {
			return;
		}

		foreach (var placement in placements) {
			var cell = placement.Cell;

			if (cell.Row < 0 || cell.Column < 0) {
				throw new ArgumentOutOfRangeException(nameof(placements), "Grid coordinates must be non-negative.");
			}

			if (cell.RowSpan < 1 || cell.ColumnSpan < 1) {
				throw new ArgumentOutOfRangeException(nameof(placements), "Grid spans must be at least 1.");
			}
		}

		var rowCount = placements.Max(placement => placement.Cell.Row + placement.Cell.RowSpan);
		var columnCount = placements.Max(placement => placement.Cell.Column + placement.Cell.ColumnSpan);
		var occupied = new bool[rowCount, columnCount];

		foreach (var placement in placements) {
			var cell = placement.Cell;

			for (var row = cell.Row; row < cell.Row + cell.RowSpan; row++) {
				for (var column = cell.Column; column < cell.Column + cell.ColumnSpan; column++) {
					if (occupied[row, column]) {
						throw new InvalidOperationException("AsciiGrid cells cannot overlap.");
					}

					occupied[row, column] = true;
				}
			}
		}
	}

	private static void MeasureColumns(IReadOnlyList<CellPlacement> placements, int[] columnWidths, int columnSpacing) {
		foreach (var placement in placements) {
			var cell = placement.Cell;
			var requiredWidth = GetRequiredWidth(cell);

			if (cell.ColumnSpan == 1) {
				columnWidths[cell.Column] = Math.Max(columnWidths[cell.Column], requiredWidth);
			}
		}

		var changed = true;

		while (changed) {
			changed = false;

			foreach (var placement in placements.Where(placement => placement.Cell.ColumnSpan > 1)) {
				var cell = placement.Cell;
				var requiredWidth = GetRequiredWidth(cell);
				var currentWidth = GetSpanLength(columnWidths, cell.Column, cell.ColumnSpan, columnSpacing);

				if (currentWidth >= requiredWidth) {
					continue;
				}

				var deficit = requiredWidth - currentWidth;
				var increase = deficit / cell.ColumnSpan;
				var remainder = deficit % cell.ColumnSpan;

				for (var i = 0; i < cell.ColumnSpan; i++) {
					columnWidths[cell.Column + i] += increase + (i < remainder ? 1 : 0);
				}

				changed = true;
			}
		}
	}

	private static void MeasureRows(IReadOnlyList<CellPlacement> placements, int[] rowHeights) {
		foreach (var placement in placements) {
			var cell = placement.Cell;
			var requiredHeight = GetRequiredHeight(cell);

			if (cell.RowSpan == 1) {
				rowHeights[cell.Row] = Math.Max(rowHeights[cell.Row], requiredHeight);
			}
		}

		var changed = true;

		while (changed) {
			changed = false;

			foreach (var placement in placements.Where(placement => placement.Cell.RowSpan > 1)) {
				var cell = placement.Cell;
				var requiredHeight = GetRequiredHeight(cell);
				var currentHeight = GetSpanLength(rowHeights, cell.Row, cell.RowSpan, 0);

				if (currentHeight >= requiredHeight) {
					continue;
				}

				var deficit = requiredHeight - currentHeight;
				var increase = deficit / cell.RowSpan;
				var remainder = deficit % cell.RowSpan;

				for (var i = 0; i < cell.RowSpan; i++) {
					rowHeights[cell.Row + i] += increase + (i < remainder ? 1 : 0);
				}

				changed = true;
			}
		}
	}

	private static int GetRequiredWidth(AsciiCell cell) {
		var lines = SplitLines(cell.Text);
		return lines.Max(line => line.Length) + cell.PaddingLeft + cell.PaddingRight;
	}

	private static int GetRequiredHeight(AsciiCell cell) {
		return SplitLines(cell.Text).Count + cell.PaddingTop + cell.PaddingBottom;
	}

	private static int GetSpanLength(int[] values, int start, int span, int separatorWidth) {
		var total = 0;

		for (var i = start; i < start + span; i++) {
			total += values[i];
		}

		total += separatorWidth * Math.Max(0, span - 1);

		return total;
	}

	private sealed record CellPlacement(int Index, AsciiCell Cell);
}