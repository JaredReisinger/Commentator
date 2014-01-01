---
title: Roadmap
layout: default
---

# Roadmap (roughly)

 * Auto-wrap comments as they are typed

  * whitespace-separated words wrap on or before the line-length limit

  * if the next line starts with a matching comment delimiter (same whitespace
    before and after the "//") the word is injected into the beginning of the
    line and that line is also wrapped

 * DocComment formatting

  * more than just the DocComment tags, the entire comment block is formatted
    to look like the MSDN-style documentation pages

  * this _might_ include bringing in type/parameter/name information from the
    related code

  * support images in DocComments?  (and maybe elsewhere?)

 * also support prefix-based formatting (+, ++, !, ?, etc.?)

 * also support limited markdown formatting?

 * perhaps make multi-line ("//", "//" or "/*...*/") comments look like blocks
   a la DocComments (above)?
