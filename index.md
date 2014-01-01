---
title: Commentator
layout: default
---

# Why another comment extension?

There are already several comment-formatting extensions for Visual Studio:
[CommentsPlus](http://visualstudiogallery.msdn.microsoft.com/f76e86e3-03ca-4ac8-ba88-58c8f8d818f4),
[redmuffin.MultiAdornment](http://visualstudiogallery.msdn.microsoft.com/03e958d5-66a5-4947-9d5e-334766cc5877),
[JR Keywords](http://visualstudiogallery.msdn.microsoft.com/a99a9ef0-aba2-4948-a74e-abbc0d1a7daa),
[DoxygenComments](http://visualstudiogallery.msdn.microsoft.com/11a30c1c-593b-4399-a702-f23a56dd8548),
[VS10x Comments Extender](http://visualstudiogallery.msdn.microsoft.com/17c68951-7743-40bd-ad35-608706f54a92),
[SharpComments](http://visualstudiogallery.msdn.microsoft.com/32b91d27-2a0f-4a4b-9ad3-caed8b4ced4b),
and others, I'm sure.  Most of these provide some basic additional handling for
comments by using a leading character to provide different formatting.

Commontator, on the other hand, seeks to make writing and updating comments a
much more natural and useful experience.  What if the process of writing or
editing comments felt more like using Word?  If adding a word or two in the
middle of a comment didn't mean deciding between jaggedly uneven line lengths
or manually reflowing to the next line (and the next, and the next).  What if
you didn't have to pay any attention to which column you were in, because what
you type automatically moves to the next line as needed?

Welcome to Commentator.

## Roadmap (roughly)

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
