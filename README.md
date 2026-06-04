# HawsLabs.Extensions

Reusable extension packages:

- `packages/system`: general-purpose extensions for .NET system types.
	- Includes `System.IO` helpers for globbing, parent directory traversal, common file base paths, relative normalized file paths, and path containment checks.
	- ASCII file-tree formatting returns an `AsciiTree` result with `RootPath` and rendered `Text`, and now uses reusable `AsciiGrid` and `AsciiCell` layout types for aligned spans.
- `packages/linqpad`: LINQPad-specific adapter extensions that depend on `LINQPad.Runtime`.
	- ASCII trees can be converted to LINQPad-renderable output with `AsciiTree.ToDump()` and then dumped with LINQPad's `.Dump()`.