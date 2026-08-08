# Compiler architecture

The SMILE 2.0 MVP pipeline is deliberately direct:

```text
.smile source -> Smile.Language -> MASM x64 -> ml64/link -> native .exe
```

The generated program links a small static native runtime for console text, keyboard input, screen reset, waits, and random numbers. No translated C/C#/Python source, VM, interpreter, LLVM layer, or second language implementation is involved.
