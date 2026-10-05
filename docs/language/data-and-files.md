# Data and files

[Language reference](README.md) · Recoverable storage, text transfer and native prompts.

## Recoverable persistent Data

Existing `Save Data` and `Load Data` statements accept an optional final
`Status` clause with a writable `Number` target. `Status` remains usable as an
identifier. Without the clause, invalid/corrupt/unavailable storage still stops
the program; a missing strict load returns zero bytes and clears its destination.

```smile
Dim Bytes[8] As Number
Dim ByteCount As Number
Dim SaveStatus As Number

Bytes[0] = 12

Save Data Bytes Count 1 To "Example" Status SaveStatus

If SaveStatus = DATA_STATUS_OK Then
    Print "Saved"
End If

Load Data "Example" Into Bytes Count ByteCount Status SaveStatus
```

| Status constant | Value | Meaning |
| --- | ---: | --- |
| `DATA_STATUS_OK` | 0 | Saved successfully or loaded the primary. |
| `DATA_STATUS_MISSING` | 1 | Neither primary nor backup exists. |
| `DATA_STATUS_RECOVERED` | 2 | Loaded a checksummed last-good backup. |
| `DATA_STATUS_INVALID` | 3 | Invalid buffer, byte values or count. |
| `DATA_STATUS_UNAVAILABLE` | 4 | Storage denied, quota, sharing, allocation or I/O failure. |
| `DATA_STATUS_CORRUPT` | 5 | No usable checksummed envelope. |
| `DATA_STATUS_TOO_LARGE` | 6 | Valid stored payload exceeds the destination capacity. |

Checked loads set Count to zero on missing/failure and leave the byte destination
unchanged. On success they replace exactly Count bytes, leaving the rest unchanged.
Count and Status outputs are assigned in that order. Recovery reads the backup
only for a missing/corrupt primary, does not rewrite it, and reports recovery
explicitly. A subsequent checked save may replace that corrupt primary only if
its backup validates; the corrupt file never replaces the good backup.

Native saves use flushed temporary files and atomic replacement. Web saves keep
the previous valid envelope in the same application/key namespace with `.bak`,
then write the primary, and only then update the runtime cache. A failed primary
write can advance the backup to the still-current primary, but cannot make an
unsaved candidate the saved memory copy. Storage is origin-specific and browser
eviction remains outside the application's control. Export important data.
These operations do not provide cross-process/tab locking or merge concurrent
edits. See the compiled `examples/DataStatusBasics.smile` teaching example.

## User-chosen UTF-8 files

`File_Export(FileName As Text, Contents As Text) As Boolean` opens a native Save As
dialog or requests a browser download. The suggested name must be a filename,
not a path (at most 200 UTF-8 bytes, no separators, control characters or Windows
reserved punctuation, and no trailing dot/space). Contents are bounded to 8 MiB.
Native `True` means the chosen file was flushed and replaced successfully; Web
`True` only means a download was requested. Browser policy and the user's choice
still determine whether it reaches disk. Cancel/failure returns `False`.

`File_Import() As Text` opens a user-controlled picker and returns UTF-8 text,
removing an optional UTF-8 BOM. Cancel, failure, invalid UTF-8, an empty file or a
file exceeding 8 MiB returns empty Text. It does not parse JSON or execute files.
Browser import/export requires an active user gesture; neither function grants
browser access to arbitrary local paths. Save Data and application identities are
unchanged. `examples/TextFileTransferBasics.smile` compiles for native and Web.

## Executable-relative text input

Generic executable-relative text input uses:

```smile
Dim FileBytes[8192]
Load Text File "Maps\default.map" Into FileBytes Count FileByteCount
```

The path may be a `Text` expression and must be non-empty, the destination must be a one-dimensional numeric array, and `Count` must name a writable numeric variable. The runtime zero-fills the complete destination, reads UTF-8 bytes, skips an optional UTF-8 BOM, copies at most the array capacity as values from 0 through 255, and stores the copied byte count. Missing, inaccessible, empty, or unreadable files safely produce count zero. Existing integer persistence keeps its distinct `Load Value From "Key" Default 0` form.

Dungeon Star I provides the complete multi-floor game-side example: three literal-path loaders feed one bounded byte parser in `games\DungeonStarI\Program.smile`. Dungeon Star II uses the same generic statement for compatible one-floor room maps in `games\DungeonStarII\Program.smile`. The language/runtime only delivers bytes; headers, symbols, dimensions, topology, support rules, and fallback behavior remain ordinary SMILE source.

## Native text prompts and editor keys

`File_Pick(Saving As Boolean, Title As Text, Extension As Text, SuggestedName As Text) As Text`
requires `Game Window`. Native Windows opens an owner-modal Save/Open dialog and
returns the selected absolute path, or empty text on cancellation. It does not read
or write file contents. `Extension` is an alphanumeric suffix without a dot. Native
Save asks before replacing an existing destination. The Web target returns empty
text because browser pages cannot expose native filesystem paths.

`Text_Prompt(Title As Text, Message As Text, Initial As Text) As Text` requires
`Game Window`. It opens an owner-modal Unicode text dialog and returns the entered
text on OK; Cancel returns empty text. The title is bounded to 256 UTF-16 code
units, message to 1,024 and initial/result to 256. An empty result can also mean
accepted empty input; callers should validate their own names. The native owner
continues painting and consumes game shortcuts while the dialog is open. Web
source uses the browser prompt; editor adoption/browser acceptance remains held.

`Text_From_Code(Code As Number) As Text` returns one Unicode scalar, or empty text
for a negative value, a surrogate code point, or a value above U+10FFFF. It pairs
with the scalar-based `Text_Code_At` and `Text_Length` operations.

`KEY_SHIFT` (43) and `KEY_DELETE` (44) are appended shared key constants. They work
with `Get Key`, `Key_Held` and queued `Key_Event_Held`; earlier numeric identities
are unchanged. The native town editor uses Shift+left drag for camera panning and
Delete for the active category selection.
