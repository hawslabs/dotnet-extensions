using HawsLabs.Extensions.LINQPad.Trees;

namespace HawsLabs.Extensions.LINQPad.Formatting;

public sealed class AsciiTreeNodeFormatter : ITreeNodeFormatter
{
    private readonly AsciiTreeNodeFormatterOptions _options;

    public AsciiTreeNodeFormatter()
        : this(null)
    {
    }

    public AsciiTreeNodeFormatter(AsciiTreeNodeFormatterOptions? options)
    {
        _options = options ?? new AsciiTreeNodeFormatterOptions();
    }

    public string Format(TreeNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var maxNameColumnWidth = GetRootLeftText(root).Length;
        var maxLineColumnWidth = FormatNumber(root.TotalLines).Length;

        Measure(root, indent: "", ref maxNameColumnWidth, ref maxLineColumnWidth);

        var sb = new StringBuilder();

        AppendNodeLine(
            sb,
            leftText: GetRootLeftText(root),
            lineCount: root.TotalLines,
            labelCount: root.TotalLabelCount,
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
        ref int maxLineColumnWidth)
    {
        var children = GetSortedChildren(node);

        foreach (var child in children)
        {
            var connector = "├── ";
            var icon = GetIcon(child);
            var leftText = indent + connector + icon + child.Name;
            var lineText = FormatNumber(GetDisplayLineCount(child));

            maxNameColumnWidth = Math.Max(maxNameColumnWidth, leftText.Length);
            maxLineColumnWidth = Math.Max(maxLineColumnWidth, lineText.Length);

            Measure(child, indent + "    ", ref maxNameColumnWidth, ref maxLineColumnWidth);
        }
    }

    private void RenderChildren(
        StringBuilder sb,
        TreeNode node,
        string indent,
        int maxNameColumnWidth,
        int maxLineColumnWidth)
    {
        var children = GetSortedChildren(node);

        for (var i = 0; i < children.Count; i++)
        {
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
        bool forceShowLabel)
    {
        var lineText = FormatNumber(lineCount);

        if (_options.AlignColumns)
        {
            sb.Append(leftText.PadRight(maxNameColumnWidth));
            sb.Append(_options.ColumnSeparator);
            sb.Append(lineText.PadLeft(maxLineColumnWidth));
        }
        else
        {
            sb.Append(leftText);
            sb.Append(_options.ColumnSeparator);
            sb.Append(lineText);
        }

        if (_options.ShowLabels && (forceShowLabel || labelCount > 0 || _options.ShowZeroLabelCounts))
        {
            sb.Append(_options.LabelSeparator);
            sb.Append(_options.LabelIcon);
            sb.Append(labelCount.ToString(_options.NumberFormat, _options.Culture));
        }

        sb.AppendLine();
    }

    private IReadOnlyList<TreeNode> GetSortedChildren(TreeNode node)
    {
        return _options.SortOrder switch
        {
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

    private string GetRootLeftText(TreeNode root)
    {
        return _options.ShowIcons
            ? _options.DirectoryIcon + root.Name
            : root.Name;
    }

    private string GetIcon(TreeNode node)
    {
        if (!_options.ShowIcons)
            return "";

        return node.IsFile
            ? _options.FileIcon
            : _options.DirectoryIcon;
    }

    private int GetDisplayLineCount(TreeNode node)
    {
        return node.IsFile
            ? node.LineCount
            : node.TotalLines;
    }

    private int GetDisplayLabelCount(TreeNode node)
    {
        return node.IsFile
            ? node.LabelCount
            : node.TotalLabelCount;
    }

    private string FormatNumber(int value)
    {
        return value.ToString(_options.NumberFormat, _options.Culture);
    }
}
