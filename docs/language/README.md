# Language implementation

`src/Smile.Language` is the sole authority for SMILE 2.0 source text, tokens, keyword facts, syntax, diagnostics, symbols, types, and semantic analysis.

Both `smilec` and the Visual Studio extension call `SmileLanguage.Analyze` and consume the returned tokens, syntax tree, diagnostics, and semantic model.
