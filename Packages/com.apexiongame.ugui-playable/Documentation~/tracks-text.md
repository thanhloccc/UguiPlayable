# TMP Text track

Menu: **UGUI Playable > Text > TMP Text**. Binds to a `TMP_Text`, which is `TextMeshProUGUI` or the world-space
`TextMeshPro`. The legacy `UnityEngine.UI.Text` is not supported; use the Graphic track for its colour.

For effects on single characters (a per-character fade, bounce or wave), see the separate track in
[tmp-character-track.md](tmp-character-track.md). The two tracks can run on the same text.

TextMeshPro needs its Essential Resources in the project. See [troubleshooting.md](troubleshooting.md).

## Channels

All tweened channels default to **Absolute** mode and a **Linear** ease. See
[value-modes-and-restore.md](value-modes-and-restore.md).

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Color | Color | white | white | off |
| Alpha | float | 0 | 1 | off |
| Font Size | float | 36 | 48 | off |
| Character Spacing | float | 0 | 10 | off |
| Word Spacing | float | 0 | 10 | off |
| Line Spacing | float | 0 | 10 | off |
| Paragraph Spacing | float | 0 | 10 | off |
| Visible Ratio (typewriter) | float, 0 to 1 | 0 | 1 | **on** |
| Color Gradient | four-corner gradient | white | white | off |
| Text (string tween) | text | empty | empty | off |

Only the typewriter channel is on in a new clip.

### Font Size

While the bound text has **Auto Size** on, TMP sets the font size itself and discards what this channel writes. The
Font Size card shows a note, and the track row and the track inspector show a warning while any clip turns Font Size
on. Turn Auto Size off to animate the size.

### Color and alpha

Both write the text colour. If both are on, Alpha decides the A and Color decides RGB, as on the graphic tracks.

### Visible Ratio (typewriter)

The ratio is a share, 0 to 1, of the text's characters. The track converts it once per frame to
`maxVisibleCharacters`, rounding down: nothing shows at the start, and the first character appears at 1/n of the ratio.
A ratio of 1 or more writes TMP's "all" value (99999), so text you set later is not cut.

The character count follows the string this track writes in the same frame (the Text channel), so the typewriter and
the string tween of one track never disagree. Text changed by other code is counted from TMP's last rebuild, which can
lag one frame behind the change. The track never forces a mesh update.

**Unit** decides what one step reveals: a **Character** (the default), a **Word** or a **Line**. A word comes with the
spaces and punctuation after it, so "Hello, brave new world." shows `Hello, ` then `Hello, brave ` and so on, and the
full stop comes with the last word. Words are TMP's: runs of letters, digits and hyphens. A text with no space between
its letters (Chinese, Japanese) can be a single word, and a text with no letter or digit at all is revealed by
character. Lines are the lines TMP laid out, wrapping included.

**Pause** waits after a step that ends with punctuation (`.` `,` `:` `;` `!` `?` `…`), counted in steps of the unit:
with Character and Pause 6, "Wait. Go!" holds after "Wait." for as long as six more letters would take. Only the last
mark of a run waits ("..." waits once), and there is no wait after the last step. 0 never waits. The clip's length is
shared over the letters and the waits, so a pause makes the typing itself faster.

Word and line boundaries come from TMP's last layout. Right after the text changes (by the Text channel of the same
track or by other code), they follow the previous text for one frame. The track never forces a mesh update to avoid
this. Each clip maps its ratio to whole steps before the clips blend, so a crossfade between a Character clip and a
Word clip, or an ease in from the text's own count, can stop inside a word. From current starts a Word or Line clip at
the last whole step the text shows.

In Edit Mode, Timeline's preview cannot revert `maxVisibleCharacters` because TMP does not serialize it. The track
restores it itself when the preview ends.

### Color Gradient

The channel writes the gradient only. TMP shows it when **Color Gradient** is turned on in the text's **Extra
Settings**. The track leaves that switch as it is. The editor shows a note when the switch is off.

### Text (string tween)

The channel reveals a string from From to To.

| Setting | Default | Meaning |
|---|---|---|
| Mode | Absolute | Absolute or From current. Relative has no meaning for text and behaves like Absolute |
| From, To | empty | The start and end strings |
| Scramble | None | None, All, Uppercase, Lowercase, Numerals or Custom |
| Custom characters | empty | The characters used when Scramble is Custom. Empty means no scramble |
| Rate | 20 for new clips, 0 in older clips | Scramble changes per second; 0 changes them only when the reveal advances. Shown when Scramble is not None |
| Ease | Linear | Shapes the reveal |

How it works:

- The text is as long as the longer of the two strings. The reveal count is the progress times that length, rounded
  down: at the start of the clip the text is exactly From (no padding, no scramble), and every position becomes To in
  an even step. Positions below the count show To, the rest keep From. At the end of the clip the text is exactly To.
- With Scramble on, a position that is not revealed yet and whose character differs from To shows a scramble
  character. The scramble character comes from the position and the progress, not from a random generator, so
  scrubbing is stable.
- A newline in either string is kept as its own character (`\r` stays `\r`); a revealed position shows To even where
  From has a newline.
- With **Rich Text** on (the default), tags are markup: they take no position, so the count runs over the characters
  you see. The tags of To come with the revealed part, and right after it come only the closing tags of To that close
  what the revealed part opened, so a style never spills onto the unrevealed part (`Hello <color=red>World</color>`:
  "World" turns red as it is revealed). Tags still ahead, such as `<pos>`, `<space>` or `<page>`, wait until they are
  revealed. `<br>`, `<sprite…>`, `<nbsp>`, `<zwsp>`, `<zwj>`, `<shy>` and `<cr>` count as one character, text inside
  `<noparse>` counts letter by letter, and a surrogate pair (an emoji) or a parsed escape such as `\n` is one
  character. The style tags of From are dropped while the tween runs, so a From current tween loses the base text's
  styles in the unrevealed part. Where the revealed part, From and scramble characters meet, a lone `\` or `<` never
  joins what follows into an escape or a tag (the tween writes `\\`, `\u005C` or `</noparse>` there, which TMP
  shows as nothing extra). TMP prints a tag it does not know as text, while the tween treats any `<…>` as a tag, so
  such text (for example `1 < 2 and 3 > 2`) appears whole when the reveal reaches it. With Rich Text off, tags are
  plain characters and the clip card shows a note.
- The channel is not blended. When clips overlap, the active clip with the highest weight decides. After a clip, the
  latest finished clip's To shows.
- From current starts from the To of the previous text clip on the track (the one that started earlier and ends last),
  or from the text the label had when the track was bound when there is none. It does not depend on the last frame
  shown, so scrubbing straight into the clip gives the same text as playing through.
- Restoring uses TMP's text setter, which re-renders reliably.

Allocation: the track writes the string through a character buffer that only grows when a longer pair of strings
appears. In the Editor, TextMeshPro's `SetCharArray` allocates; see
[value-modes-and-restore.md](value-modes-and-restore.md).

### Counter (number)

The channel counts a number from From to To and writes it as formatted text. It is off by default; the inspector card
follows in the next release, so for now set it from code (`TmpTextBehaviour.Counter`, a `CounterChannel`).

| Setting | Default | Meaning |
|---|---|---|
| Mode | Absolute | Absolute or From current. Relative behaves like Absolute |
| From, To | 0 and 100 | The start and end values (doubles, exact up to 2^53) |
| Ease | Linear | Shapes the count |
| Decimals | 0 | 0 to 6; values round half away from zero |
| Grouping | off | Groups the integer digits by three |
| Group separator, Decimal separator | `,` and `.` | Null or empty reads as the default |
| Prefix, Suffix | empty | Text around the number; the sign comes before the prefix (`-$5`) |

How it works:

- The text is formatted without the culture, so every machine prints the same characters. A value beyond the `long`
  range (after scaling by the decimals) is clamped; `-0` and NaN print `0`.
- The counter writes through the same character buffer as the string tween and calls `SetCharArray` only when the
  digits change. The typewriter counts the formatted text, prefix and suffix included (rich-text tags in a prefix or
  suffix count as characters too).
- The counter never reads or parses the label's text. From current starts from the last value the track wrote, or
  from From when it wrote none, so a count that continues a previous clip is seamless. The start therefore depends
  on what was played or scrubbed before (and, for clips that touch, on the frame rate): jumping straight into a From
  current clip before the track wrote any value starts it from From.
- Overlapping counter clips blend their values by weight (in double); the format comes from the clip with the highest
  weight. After a clip, the latest finished counter clip's To shows.
- The string tween wins over the counter: while any active clip has the Text channel on, the string decides the
  text, even when a counter clip weighs more. With no active text or counter clip, the hold that ends later wins; on
  a tie the string tween.
- Restoring is the same as for the string tween: the base text returns whenever the text was written.

Allocation: the counter allocates nothing in the package. In the Editor, TextMeshPro's `SetCharArray` allocates when
the digits change; see [value-modes-and-restore.md](value-modes-and-restore.md).

## Restore

Only the properties the mixer wrote are restored. With **Revert**, the colour, sizes, spacing and gradient return to
their base values, and the base text returns whenever the text was written.

Next: [tmp-character-track.md](tmp-character-track.md).
