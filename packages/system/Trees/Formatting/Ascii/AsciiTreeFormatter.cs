namespace System.Trees.Formatting.Ascii;

using Nodes;

public sealed class AsciiTreeFormatter(
	AsciiTreeFormatterOptions options
) : ITreeNodeFormatter {
    public static readonly AsciiTreeFormatter Default = new(
	    options: AsciiTreeFormatterOptions.Default
	);

	public string Format(TreeNode root) {
		ArgumentNullException.ThrowIfNull(root);

		var maxNameColumnWidth = GetRootLeftText(root).Length;
		var maxLineColumnWidth = options.ShowLineCounts
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

			if (options.ShowLineCounts) {
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
		var showLabel = options.ShowLabels && (forceShowLabel || labelCount > 0 || options.ShowZeroLabelCounts);

		if (options.ShowLineCounts) {
			var lineText = FormatLineCount(lineCount, options.AlignColumns ? maxLineColumnWidth : 0);

			if (options.AlignColumns) {
				sb.Append(leftText.PadRight(maxNameColumnWidth));
				sb.Append(options.ColumnSeparator);
				sb.Append(lineText);
			} else {
				sb.Append(leftText);
				sb.Append(options.ColumnSeparator);
				sb.Append(lineText);
			}
		} else if (options.AlignColumns && showLabel) {
			sb.Append(leftText.PadRight(maxNameColumnWidth));
		} else {
			sb.Append(leftText);
		}

		if (showLabel) {
			sb.Append(options.LabelSeparator);
			sb.Append(options.LabelIcon);
			sb.Append(labelCount.ToString(options.NumberFormat, options.Culture));
		}

		sb.AppendLine();
	}

	private IReadOnlyList<TreeNode> GetSortedChildren(TreeNode node) {
		return options.SortOrder switch {
			TreeSortOrder.AlphabeticalDirectoriesFirst => node.Children.Values
				.OrderBy(child => child is FileTreeNode)
				.ThenBy(child => child.Name, options.NameComparer)
				.ToList(),

			TreeSortOrder.LinesOfCodeDesc => node.Children.Values
				.OrderByDescending(GetDisplayLineCount)
				.ThenBy(child => child.Name, options.NameComparer)
				.ToList(),

			TreeSortOrder.LabelCountDesc => node.Children.Values
				.OrderByDescending(GetDisplayLabelCount)
				.ThenBy(child => child.Name, options.NameComparer)
				.ToList(),

			_ => node.Children.Values
				.OrderBy(child => child.Name, options.NameComparer)
				.ToList(),
		};
	}

	private string GetRootLeftText(TreeNode root) {
		return options.ShowIcons
			? options.DirectoryIcon + root.Name
			: root.Name;
	}

	private string GetIcon(TreeNode node) {
		if (!options.ShowIcons) {
			return "";
		}

		return node is FileTreeNode
			? options.FileIcon
			: options.DirectoryIcon;
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
		return value.ToString(options.NumberFormat, options.Culture);
	}

	private string FormatLineCount(int value, int minNumberWidth) {
		var number = FormatNumber(value).PadLeft(minNumberWidth);

		return string.IsNullOrEmpty(options.LineCountIcon)
			? number
			: options.LineCountIcon + number;
	}
}