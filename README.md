# HawsLabs.Extensions

Reusable extension packages:

- `packages/system`: general-purpose extensions for .NET system types.
	- Includes `System.IO` helpers for globbing, parent directory traversal, common file base paths, relative normalized file paths, and path containment checks.
	- ASCII file-tree formatting returns an `AsciiTree` result with `RootPath` and rendered `Text`.
- `packages/linqpad`: LINQPad-specific adapter extensions that depend on `LINQPad.Runtime`.
	- LINQPad dumping is available from `AsciiTree.DumpAsAsciiTree()`.