# Non-Local Resource Unwind Proof

These focused fixtures prove deterministic cleanup across every active native and Web routine frame when execution terminates through `End Program`, a runtime `Nothing` receiver, or the internal Class-allocation fault injection.

Each recursive frame owns ordinary Text, Image, Type, and Class values. The test gate also covers staged arguments, property evaluation order, and constructor arguments. Native runs require zero Text, Image, and Class lifetime diagnostics. Web runs require exact visible output and zero retained Class/Image diagnostics.

The test-only Class allocation environment variable is an internal runtime diagnostic mechanism. It is not SMILE syntax or a public library API.
