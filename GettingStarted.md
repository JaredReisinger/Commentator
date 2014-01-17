---
title: Getting Started
layout: default
---

# Getting Started

There's not much to getting started with Commentator... once you've installed it
into Visual Studio and restarted, it's running by default.  Commentator does its
best to only kick in when it's confident it should be wrapping a comment.  If at
any time it seems to be misbehaving, please log an issue on GitHub.  If you need
to turn Commentator off, you can do so via Tools.Options setting "**Enable
automatic wrapping**".

## Auto-wrapping Concepts

### Comment Paragraphs

The basic unit for the auto-wrapping functionality is the "comment paragraph".
This isn't something which really exists in most programming language comment
definitions, or in Visual Studio.  Commentator infers the paragraph using
the same heuristics as a person would when reading the code.  Comments are
considered a part of the same "comment paragraph" if the following four things
are true:

 * the comments are on contiguous lines
 * the comment markers (`//` or `''`) start in the same column in each line
 * the comment markers are identical
 * the comment content starts in the same column in each line

Since a picture (so to speak) is worth a thousand words:

    // This is a comment paragraph.
    // And this line is a part of it
    // too, and will wrap with it.
    
    // This is the second paragraph,
    // because there is a blank line
    // before and after.
    
    // This comment block has two
    // paragraphs...
    //
    // This is the second paragraph
    // in this block, because of the
    // "blank" line between them
    
    // This is a paragraph.  It has
    // two lines in it.
        // This is a separate paragraph,
        // because the marker ("//")
        // starts at a different column.
    
    // This is a paragraph.
    //   This is a separate paragraph
    //   because it is indented *after*
    //   the comment marker.
    // And this is the third paragraph
    // in this comment block.
    
    // This is a paragraph.
    /* This is a separate paragraph,
       because it uses a different
       comment marker.
    */

Commentator also tries to play nicely with the other comment extensions for
Visual Studio, and treats `//+`, `//-`, `//!`, and `//?` as distinct comment
markers.  This also means that it will automatically insert those markers if it
needs to create a new line when wrapping, so that the paragraph should continue
to be formatted however the other extension handles it.  (A potential future
feature is for these marker modifiers to be a user-configurable list.)

### Leading Lines

It's fairly common (especially in corporate or project environments) to have a
required comment format at the top of a file.  Commentator has a setting to
avoid auto-wrapping for these lines, because they often don't follow the same
heuristics as a "comment paragraph" would.  For example, if a file starts with:

    // -------------------------------------------------------------
    // Copyright (c) [year] [company name]. All rights reserved.
    // File: [filename]
    // Owner: [owner]
    //
    // [description of file contents...]
    // -------------------------------------------------------------
    
    namespace Whatever
    {
        // . . .

In this case, you'd likely want to avoid wrapping on lines 1-4 (or 1-5), and
begin wrapping starting with the description on line 6.  To get this behavior
set the "**Avoid wrapping before line**" setting to 6, or the line on which you
want wrapping to start.  Setting this value to 1 (the default) will allow
wrapping even at the very beginning of the file.

### Code Lines

In general, comments _following_---but on the same line as---code should be very
concise, and not need wrapping.  By default, comments on "code lines" are not
wrapped.  If, however, you tend to wax poetic on these kinds of comments, and
you want wrapping to kick in, you can turn on the "**Wrap on lines with code**"
setting.  Be aware that at present this might cause consecutive comments on
consecutive code lines to be considered a single paragraph, which is almost
certainly _not_ the behavior you'd expect.