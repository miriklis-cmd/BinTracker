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
