<Query Kind="Program">
  <Reference Relative="..\..\.artifacts\bin\linqpad\debug\HawsLabs.Extensions.LINQPad.dll">D:\hawslabs\extensions\.artifacts\bin\linqpad\debug\HawsLabs.Extensions.LINQPad.dll</Reference>
  <Namespace>HawsLabs.Extensions.LINQPad.Collections</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Formatting</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Formatting.AsciiTree</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.IO</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Trees</Namespace>
</Query>

void Main() {
	var solutionDirectory = new FileInfo(Util.CurrentScriptPath)
		.Directory
		?.GetParentDirectoryOrRoot(
			levels: 2
		)
		.Dump("SolutionDirectory");
		
	if (solutionDirectory is null) {
		return;
	}
		
	solutionDirectory
		.Glob(
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
		)
		.DumpAsAsciiTree(solutionDirectory.FullName, new() {
			SortOrder = TreeSortOrder.Alphabetical,
			ShowIcons = true,
			ShowLabels = false,
			AlignColumns = true,
		});
}
