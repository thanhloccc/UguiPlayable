# Third-Party Notices

UGUI Playable contains code ported from, or ordered after, the projects below. Each ported file keeps a
`// Ported from …` header naming its source. Each section gives the source, what was taken, and the full licence
text.

## EncosyTower

- Source: https://github.com/laicasaane/EncosyTower
- License: MIT, Copyright (c) 2026 Laicasaane

Ported files (paths relative to `Packages/com.laicasaane.encosy-tower/`):

- `ApexionGame.UguiPlayable/Internal/ThrowHelper.cs` from `EncosyTower.Core/Debugging/ThrowHelper.cs`
- `ApexionGame.UguiPlayable/Internal/UguiPlayableUnityObjectExtensions.cs` from
  `EncosyTower.Core/UnityEngine.Extensions/EncosyUnityObjectExtensions.cs`
- `ApexionGame.UguiPlayable/Internal/System.Runtime.CompilerServices/CallerArgumentExpressionAttribute.cs` from
  `EncosyTower.Core/System.Runtime.CompilerServices/CallerArgumentExpressionAttribute.cs`
- `ApexionGame.UguiPlayable/Internal/System.Diagnostics/StackTraceHiddenAttribute.cs` from
  `EncosyTower.Core/System.Diagnostics/StackTraceHiddenAttribute.cs`
- `ApexionGame.UguiPlayable.Editor/Internal/System.Runtime.CompilerServices/IsExternalInit.cs` from
  `EncosyTower.Core/System.Runtime.CompilerServices/IsExternalInit.cs`
- `ApexionGame.UguiPlayable.Editor/Internal/EditorAssets.cs` from `EncosyTower.Core/Editor/EditorAPI.cs`
- `ApexionGame.UguiPlayable.Editor/Internal/ThrowHelper.cs` from `EncosyTower.Core/Editor/ThrowHelper.cs` and
  `EncosyTower.Core/Debugging/ThrowHelper.cs`

```text
MIT License

Copyright (c) 2026 Laicasaane

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## .NET (dotnet/runtime)

- Source: https://github.com/dotnet/runtime
- License: MIT, Copyright (c) .NET Foundation and Contributors

These files came in through the EncosyTower ports above and keep their .NET Foundation headers:

- `ApexionGame.UguiPlayable/Internal/ThrowHelper.cs`
- `ApexionGame.UguiPlayable/Internal/System.Diagnostics/StackTraceHiddenAttribute.cs`
- `ApexionGame.UguiPlayable/Internal/System.Runtime.CompilerServices/CallerArgumentExpressionAttribute.cs`
- `ApexionGame.UguiPlayable.Editor/Internal/System.Runtime.CompilerServices/IsExternalInit.cs`

```text
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## TweenPlayables

- Source: https://github.com/AnnulusGames/TweenPlayables
- License: MIT, Copyright (c) 2023 Annulus Games

No code was copied. `ScrambleMode` (`ApexionGame.UguiPlayable/Core/ScrambleMode.cs`) and the easing enum
(`ApexionGame.UguiPlayable/Easing/Ease.cs`) repeat its member order.

```text
MIT License

Copyright (c) 2023 Annulus Games

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Lucide

- Source: https://lucide.dev (npm package `lucide-static` 1.68.0)
- License: ISC, Copyright (c) 2026 Lucide Icons and Contributors. Some icons derive from Feather (MIT,
  Copyright (c) 2013-present Cole Bemis); both licenses are reproduced below.

Icons used (rendered to PNG in dark and light colours at 16 px and 32 px (`@2x`), under
`ApexionGame.UguiPlayable.Editor/Common/Icons/`; the 32 px files are used on high-DPI screens): `layers`, `move`, `palette`, `image`, `link`, `unlink`,
`pipette`, `chart-spline`, `scaling`, `rotate-cw`, `scale-3d`, `crosshair`, `anchor`, `blend`, `percent`,
`mouse-pointer-click`, `shield-check`, `triangle-alert`, `chevron-down`, `chevron-right`, `sliders-horizontal`, `gallery-vertical`, `scroll`, `chevrons-left-right`, `wallpaper`, `frame`, `copy`, `ruler`, `columns-3`, `grid-3x3`, `move-horizontal`, `type`, `case-sensitive`, `space`, `baseline`, `pilcrow`, `text-cursor`, `paintbrush`, `whole-word`, `activity`, `vibrate`, `dices`, `toggle-right`, `text-select`, `wand-sparkles`, `library`, `list-ordered`. `move`, `link`,
`crosshair`, `percent`, `chevron-down`, `chevron-right` and `type` are Feather-derived.

The samples use these icons, rendered white at 64 px:

- the UGUI Playable Sample uses `x` and `arrow-left`. In this repository they live in
  `Assets/Samples/UGUI Playable/<version>/ApexionGame.UguiPlayable.Samples/Art/Icons/`; the released package ships
  them in `Samples~/ApexionGame.UguiPlayable.Samples/Art/Icons/`.
- the Showcase sample uses `type`, `palette`, `layout-grid`, `sliders-horizontal`, `sparkles` and `rotate-ccw`
  (`type` is Feather-derived). In this repository they live in
  `Assets/Samples/UGUI Playable/<version>/ApexionGame.UguiPlayable.Samples.Showcase/Art/Icons/`; the released
  package ships them in `Samples~/ApexionGame.UguiPlayable.Samples.Showcase/Art/Icons/`.

```text
ISC License

Copyright (c) 2026 Lucide Icons and Contributors

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

---

The following Lucide icons are derived from the Feather project:

airplay, alert-circle, alert-octagon, alert-triangle, aperture, arrow-down-circle, arrow-down-left, arrow-down-right, arrow-down, arrow-left-circle, arrow-left, arrow-right-circle, arrow-right, arrow-up-circle, arrow-up-left, arrow-up-right, arrow-up, at-sign, calendar, cast, check, chevron-down, chevron-left, chevron-right, chevron-up, chevrons-down, chevrons-left, chevrons-right, chevrons-up, circle, clipboard, clock, code, columns, command, compass, corner-down-left, corner-down-right, corner-left-down, corner-left-up, corner-right-down, corner-right-up, corner-up-left, corner-up-right, crosshair, database, divide-circle, divide-square, dollar-sign, download, external-link, feather, frown, hash, headphones, help-circle, info, italic, key, layout, life-buoy, link-2, link, loader, lock, log-in, log-out, maximize, meh, minimize, minimize-2, minus-circle, minus-square, minus, monitor, moon, more-horizontal, more-vertical, move, music, navigation-2, navigation, octagon, pause-circle, percent, plus-circle, plus-square, plus, power, radio, rss, search, server, share, shopping-bag, sidebar, smartphone, smile, square, table-2, tablet, target, terminal, trash-2, trash, triangle, tv, type, upload, x-circle, x-octagon, x-square, x, zoom-in, zoom-out

The MIT License (MIT) (for the icons listed above)

Copyright (c) 2013-present Cole Bemis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Unbounded

- Source: https://github.com/googlefonts/unbounded, commit `f3ec43228a864a72487e41552e2140efab9884ea`
- License: SIL Open Font License 1.1, Copyright 2022 The Unbounded Project Authors
  (https://github.com/googlefonts/unbounded)

The Showcase sample uses the Unbounded Bold font (`fonts/ttf/Unbounded-Bold.ttf`, unmodified) and a TextMeshPro font
asset (`Unbounded-Bold SDF.asset`) generated from it. The font is not sold by itself. The font file, the generated font
asset and the licence (`OFL.txt`) live in
`Assets/Samples/UGUI Playable/<version>/ApexionGame.UguiPlayable.Samples.Showcase/Art/Fonts/` in this repository; the
released package ships them in `Samples~/ApexionGame.UguiPlayable.Samples.Showcase/Art/Fonts/`. The full licence text
follows.

```text
Copyright 2022 The Unbounded Project Authors (https://github.com/googlefonts/unbounded)

This Font Software is licensed under the SIL Open Font License, Version 1.1.
This license is copied below, and is also available with a FAQ at:
https://scripts.sil.org/OFL


-----------------------------------------------------------
SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007
-----------------------------------------------------------

PREAMBLE
The goals of the Open Font License (OFL) are to stimulate worldwide
development of collaborative font projects, to support the font creation
efforts of academic and linguistic communities, and to provide a free and
open framework in which fonts may be shared and improved in partnership
with others.

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The
fonts, including any derivative works, can be bundled, embedded,
redistributed and/or sold with any software provided that any reserved
names are not used by derivative works. The fonts and derivatives,
however, cannot be released under any other type of license. The
requirement for fonts to remain under this license does not apply
to any document created using the fonts or their derivatives.

DEFINITIONS
"Font Software" refers to the set of files released by the Copyright
Holder(s) under this license and clearly marked as such. This may
include source files, build scripts and documentation.

"Reserved Font Name" refers to any names specified as such after the
copyright statement(s).

"Original Version" refers to the collection of Font Software components as
distributed by the Copyright Holder(s).

"Modified Version" refers to any derivative made by adding to, deleting,
or substituting -- in part or in whole -- any of the components of the
Original Version, by changing formats or by porting the Font Software to a
new environment.

"Author" refers to any designer, engineer, programmer, technical
writer or other person who contributed to the Font Software.

PERMISSION & CONDITIONS
Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify,
redistribute, and sell modified and unmodified copies of the Font
Software, subject to the following conditions:

1) Neither the Font Software nor any of its individual components,
in Original or Modified Versions, may be sold by itself.

2) Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.

3) No Modified Version of the Font Software may use the Reserved Font
Name(s) unless explicit written permission is granted by the corresponding
Copyright Holder. This restriction only applies to the primary font name as
presented to the users.

4) The name(s) of the Copyright Holder(s) or the Author(s) of the Font
Software shall not be used to promote, endorse or advertise any
Modified Version, except to acknowledge the contribution(s) of the
Copyright Holder(s) and the Author(s) or with their explicit written
permission.

5) The Font Software, modified or unmodified, in part or in whole,
must be distributed entirely under this license, and must not be
distributed under any other license. The requirement for fonts to
remain under this license does not apply to any document created
using the Font Software.

TERMINATION
This license becomes null and void if any of the above conditions are
not met.

DISCLAIMER
THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE
COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.
```
