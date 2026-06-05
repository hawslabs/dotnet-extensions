namespace System.Trees.Parsing;

using System.Trees.Nodes;

public sealed class TreeNodeParserResolver {
	private readonly IReadOnlyList<ITreeNodeParser> parsers;

	public TreeNodeParserResolver(IEnumerable<ITreeNodeParser> parsers) {
		ArgumentNullException.ThrowIfNull(parsers);

		this.parsers = parsers.ToArray();
	}

	public TreeNode Parse(TreeNodeParseContext context) {
		ArgumentNullException.ThrowIfNull(context);

		var matches = parsers
			.Select(parser => new ParserCandidate(parser, parser.Match(context)))
			.Where(candidate => candidate.Match.IsMatch)
			.OrderByDescending(candidate => candidate.Match.Specificity)
			.ThenByDescending(candidate => candidate.Parser.Priority)
			.ToArray();

		if (matches.Length == 0) {
			throw new InvalidOperationException($"No parser matched '{context.File.FullName}'.");
		}

		var best = matches[0];

		var ambiguousMatches = matches
			.Where(candidate =>
				candidate.Match.Specificity == best.Match.Specificity
			 && candidate.Parser.Priority == best.Parser.Priority
			)
			.ToArray();

		if (ambiguousMatches.Length > 1) {
			var parserNames = string.Join(
				", ",
				ambiguousMatches.Select(candidate => FormatParserMatch(candidate))
			);

			throw new InvalidOperationException(
				$"Ambiguous parser match for '{context.File.FullName}'. "
			  + $"Specificity {best.Match.Specificity} and priority {best.Parser.Priority} matched by: {parserNames}."
			);
		}

		return best.Parser.Parse(context);
	}

	private static string FormatParserMatch(ParserCandidate candidate) {
		var parserName = candidate.Parser.GetType().Name;

		return string.IsNullOrWhiteSpace(candidate.Match.Reason)
			? parserName
			: $"{parserName} ({candidate.Match.Reason})";
	}

	private readonly record struct ParserCandidate(
		ITreeNodeParser Parser,
		TreeNodeParserMatch Match
	);
}
