---
title: Release Notes
layout: default
---

# Release Notes

## v0.2.3 (12-Jan-2014)

Fixed issue where leading tabs were counted as _one_ character/column rather
than using the current view's settings.

## v0.2.1 (01-Jan-2014)

Found lingering reference to improperly named "Comm**o**ntator" instead of the
correct "Comm**e**ntator", sadly in the Help.About string!  Fixed and re-published.


## v0.2.0 (01-Jan-2014)

First public release.

Only single-line comments ("//"-to-end-of-line) are wrapped to ensure DocComment-style
and other comments aren't accidentally flattened.  (First, do no harm!)

Overall auto-wrapping feature and wrap-column can be configured in Tools.Options.
