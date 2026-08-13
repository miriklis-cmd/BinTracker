# BinTracker Current Release Notes

## v0.4.0-alpha.19.8

### Reverse-side one-page correction
Alpha.19.7 incorrectly selected reverse sizing from raw row count and increased the normal-day reverse font to 8.4pt. The real workbook demonstrated that wrapped CREDIT rows made the physical height larger than the row count predicted.

Alpha.19.8:
- caps reverse normal-day type at the 8.0pt size already proven to fit in alpha.19.6;
- estimates physical rendered-line load rather than only counting records;
- treats likely long-buyer and CREDIT wrapping as additional vertical load;
- progressively reduces font and padding as that load rises;
- gives the reverse Total column more width to prevent CREDIT wrapping in the first place.

The front-page improvements from alpha.19.7 are retained.
