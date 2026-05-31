<Query Kind="Program">
  <Reference Relative="..\..\.artifacts\bin\linqpad\debug\HawsLabs.Extensions.LINQPad.dll">D:\hawslabs\extensions\.artifacts\bin\linqpad\debug\HawsLabs.Extensions.LINQPad.dll</Reference>
  <Namespace>HawsLabs.Extensions.LINQPad.Collections</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Formatting</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Formatting.AsciiTree</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.IO</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Trees</Namespace>
</Query>

void Main() {
	var basePath = Path.GetRelativePath(Path.GetDirectoryName(Util.CurrentScriptPath), Path.Combine("..", "..")).Dump();

	var filePaths = FileTree.EnumerateFiles(
		basePath: basePath,
		includePatterns:
		[
			"**/*.{csproj,sln,slnx,props,targets}",
		],
		excludePatterns:
		[
			".git",
			"**/*.Designer.cs",
			"**/*DbContextModelSnapshot.cs",
			"**/node_modules/",
			"**/{artifacts,.artifacts,obj,bin,.vs}/",
		],
		options: new FileEnumerationOptions {
			SortResults = true,
			ExpandBracePatterns = true,
			TreatBareExcludeAsDirectoryName = true,
		}
	).DumpAsAsciiTree(basePath, new() {
		SortOrder = TreeSortOrder.Alphabetical,
		ShowIcons = true,
		ShowLabels = true,
		AlignColumns = true,
	});
}
