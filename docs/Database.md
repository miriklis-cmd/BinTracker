# BinTracker Database

## ApplicationSettings business information

The singleton `ApplicationSettings` row also stores configurable Business Information:

- BusinessName
- TradingName
- Abn
- Address
- Phone
- Email
- DefaultReportHeader

Schema migration v8 adds these columns to existing SQLite databases without replacing the settings row.


## Branding schema status

The current schema stores textual Business Information only. There is no logo/blob/path field yet. Logo persistence and migration design are part of the pre-v1 Business Information & Branding milestone and must be decided before schema implementation.
