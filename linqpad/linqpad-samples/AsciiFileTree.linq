<Query Kind="Program">
  <Reference Relative="..\..\.artifacts\bin\linqpad\debug\HawsLabs.Extensions.LINQPad.dll">D:\hawslabs\extensions\.artifacts\bin\linqpad\debug\HawsLabs.Extensions.LINQPad.dll</Reference>
  <Namespace>HawsLabs.Extensions.LINQPad.Collections</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Formatting</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Formatting.AsciiTree</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.IO</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Trees</Namespace>
</Query>

void Main() {
	Util.GitRootDirectory?.Glob(
		includePatterns: [
			"**/*.{csproj,sln,slnx,props,targets}",
			"**/*.{cs,json}",
		],
		excludePatterns: [
			".git",
			"**/*.Designer.cs",
			"**/*DbContextModelSnapshot.cs",
			"**/node_modules/",
			"**/{artifacts,.artifacts,obj,bin,.vs}/",
		]
	).DumpAsAsciiTree(Util.GitRootDirectory!.FullName, new() {
		SortOrder = TreeSortOrder.Alphabetical,
		ShowIcons = true,
		ShowLabels = false,
		ShowLineCounts = false,
		AlignColumns = true,
	});
}
