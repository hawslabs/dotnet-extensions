namespace System.Trees.Formatting.Ascii;

using Nodes;

public sealed class AsciiTreeFormatter(
	AsciiTreeFormatterOptions? options = null
) : ITreeNodeFormatter {
	private readonly AsciiTreeFormatterOptions _options
		= options ?? new AsciiTreeFormatterOptions();

	public string Format(TreeNode root) {
		ArgumentNullException.ThrowIfNull(root);

		var maxNameColumnWidth = GetRootLeftText(root).Length;
		var maxLineColumnWidth = _options.ShowLineCounts
			? FormatNumber(GetDisplayLineCount(root)).Length
			: 0;

		Measure(root, indent: "", ref maxNameColumnWidth, ref maxLineColumnWidth);

		var sb = new StringBuilder();

		AppendNodeLine(
			sb,
			leftText: GetRootLeftText(root),
			lineCount: GetDisplayLineCount(root),
			labelCount: GetDisplayLabelCount(root),
			maxNameColumnWidth,
			maxLineColumnWidth,
			forceShowLabel: true
		);

		RenderChildren(
			sb,
			root,
			indent: "",
			maxNameColumnWidth,
			maxLineColumnWidth
		);

		return sb.ToString();
	}

	private void Measure(
		TreeNode node,
		string indent,
		ref int maxNameColumnWidth,
		ref int maxLineColumnWidth
	) {
		var children = GetSortedChildren(node);

		foreach (var child in children) {
			var connector = "├── ";
			var icon = GetIcon(child);
			var leftText = indent + connector + icon + child.Name;

			maxNameColumnWidth = Math.Max(maxNameColumnWidth, leftText.Length);

			if (_options.ShowLineCounts) {
				var lineText = FormatNumber(GetDisplayLineCount(child));
				maxLineColumnWidth = Math.Max(maxLineColumnWidth, lineText.Length);
			}

			Measure(child, indent + "    ", ref maxNameColumnWidth, ref maxLineColumnWidth);
		}
	}

	private void RenderChildren(
		StringBuilder sb,
		TreeNode node,
		string indent,
		int maxNameColumnWidth,
		int maxLineColumnWidth
	) {
		var children = GetSortedChildren(node);

		for (var i = 0; i < children.Count; i++) {
			var child = children[i];
			var isLast = i == children.Count - 1;

			var connector = isLast ? "└── " : "├── ";
			var leftText = indent + connector + GetIcon(child) + child.Name;

			AppendNodeLine(
				sb,
				leftText,
				GetDisplayLineCount(child),
				GetDisplayLabelCount(child),
				maxNameColumnWidth,
				maxLineColumnWidth,
				forceShowLabel: false
			);

			var childIndent = indent + (isLast ? "    " : "│   ");
			RenderChildren(sb, child, childIndent, maxNameColumnWidth, maxLineColumnWidth);
		}
	}

	private void AppendNodeLine(
		StringBuilder sb,
		string leftText,
		int lineCount,
		int labelCount,
		int maxNameColumnWidth,
		int maxLineColumnWidth,
		bool forceShowLabel
	) {
		var showLabel = _options.ShowLabels && (forceShowLabel || labelCount > 0 || _options.ShowZeroLabelCounts);

		if (_options.ShowLineCounts) {
			var lineText = FormatLineCount(lineCount, _options.AlignColumns ? maxLineColumnWidth : 0);

			if (_options.AlignColumns) {
				sb.Append(leftText.PadRight(maxNameColumnWidth));
				sb.Append(_options.ColumnSeparator);
				sb.Append(lineText);
			} else {
				sb.Append(leftText);
				sb.Append(_options.ColumnSeparator);
				sb.Append(lineText);
			}
		} else if (_options.AlignColumns && showLabel) {
			sb.Append(leftText.PadRight(maxNameColumnWidth));
		} else {
			sb.Append(leftText);
		}

		if (showLabel) {
			sb.Append(_options.LabelSeparator);
			sb.Append(_options.LabelIcon);
			sb.Append(labelCount.ToString(_options.NumberFormat, _options.Culture));
		}

		sb.AppendLine();
	}

	private IReadOnlyList<TreeNode> GetSortedChildren(TreeNode node) {
		return _options.SortOrder switch {
			TreeSortOrder.AlphabeticalDirectoriesFirst => node.Children.Values
				.OrderBy(child => child is FileTreeNode)
				.ThenBy(child => child.Name, _options.NameComparer)
				.ToList(),

			TreeSortOrder.LinesOfCodeDesc => node.Children.Values
				.OrderByDescending(GetDisplayLineCount)
				.ThenBy(child => child.Name, _options.NameComparer)
				.ToList(),

			TreeSortOrder.LabelCountDesc => node.Children.Values
				.OrderByDescending(GetDisplayLabelCount)
				.ThenBy(child => child.Name, _options.NameComparer)
				.ToList(),

			_ => node.Children.Values
				.OrderBy(child => child.Name, _options.NameComparer)
				.ToList(),
		};
	}

	private string GetRootLeftText(TreeNode root) {
		return _options.ShowIcons
			? _options.DirectoryIcon + root.Name
			: root.Name;
	}

	private string GetIcon(TreeNode node) {
		if (!_options.ShowIcons) {
			return "";
		}

		return node is FileTreeNode
			? _options.FileIcon
			: _options.DirectoryIcon;
	}

	private int GetDisplayLineCount(TreeNode node) {
		return node switch {
			FileTreeNode file => file.LineCount,
			FolderTreeNode folder => folder.TotalLines,
			_ => 0,
		};
	}

	private int GetDisplayLabelCount(TreeNode node) {
		return node switch {
			FileTreeNode file => file.LabelCount,
			FolderTreeNode folder => folder.TotalLabelCount,
			_ => 0,
		};
	}

	private string FormatNumber(int value) {
		return value.ToString(_options.NumberFormat, _options.Culture);
	}

	private string FormatLineCount(int value, int minNumberWidth) {
		var number = FormatNumber(value).PadLeft(minNumberWidth);

		return string.IsNullOrEmpty(_options.LineCountIcon)
			? number
			: _options.LineCountIcon + number;
	}
}