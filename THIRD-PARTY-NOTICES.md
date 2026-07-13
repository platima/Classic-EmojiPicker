# Third-Party Notices

Classic Emoji Picker is distributed under the [MIT License](LICENSE) and includes
or depends on the following third-party components. Each remains under its own
licence, reproduced or linked below.

## Bundled data

### emojibase
- **Used for:** the search keyword/tag data in `EmojiPicker/Resources/keywords.json`
  (merged with emojilib, below), generated from emojibase's annotations
  (which derive from Unicode CLDR).
- **Author:** Miles Johnson and contributors
- **Licence:** MIT — https://github.com/milesj/emojibase/blob/master/LICENSE
- **Project:** https://github.com/milesj/emojibase

### emojilib
- **Used for:** community keyword associations merged into
  `EmojiPicker/Resources/keywords.json` (e.g. "laugh" for 😅), giving search
  coverage similar to the Windows 10 panel.
- **Author:** Mu-An Chiou and contributors
- **Licence:** MIT — https://github.com/muan/emojilib/blob/main/LICENSE
- **Project:** https://github.com/muan/emojilib

### Unicode emoji frequency data
- **Used for:** the search-result popularity ordering in
  `EmojiPicker/Resources/popularity.json`, parsed from Unicode's published
  emoji-frequency tiers and supplemented with popular post-2018 emoji absent
  from that dataset.
- **Author:** Unicode, Inc.
- **Licence:** Unicode License — https://www.unicode.org/license.txt
- **Source:** https://home.unicode.org/emoji/emoji-frequency/

## NuGet package dependencies

These are restored at build time and bundled into the published executable.

### Emoji.Wpf
- **Used for:** colour emoji rendering via the system Segoe UI Emoji font.
- **Author:** Sam Hocevar
- **Licence:** WTFPL — see the package's licence at https://github.com/samhocevar/emoji.wpf
- **Project:** https://github.com/samhocevar/emoji.wpf

### VirtualizingWrapPanel
- **Used for:** UI virtualization of the emoji grid.
- **Author:** Sebastian Bäumlisberger
- **Licence:** MIT — https://github.com/sbaeumlisberger/VirtualizingWrapPanel/blob/master/LICENSE
- **Project:** https://github.com/sbaeumlisberger/VirtualizingWrapPanel

## System components (not redistributed)

- **Segoe UI Emoji** — Microsoft's colour emoji font, used for rendering at runtime.
  It ships with Windows (10 version 1809+ and Windows 11) and is **not** bundled with
  or redistributed by this project.
