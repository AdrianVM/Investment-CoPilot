# Spec Delta

## Purpose

Provides the current fundamentals of a stock, looked up by ticker symbol, from an interchangeable market data provider, so that users and the assessment features can see the numbers behind the investment criteria.

## ADDED Requirements

### Requirement: Stock fundamentals can be looked up by ticker symbol
The system SHALL return a stock's fundamentals for a ticker symbol to any authenticated user. The response contains the symbol, company name, currency, price, market capitalisation, trailing P/E, forward P/E, EPS growth, institutional ownership, insider ownership, debt-to-equity, the name of the data source, and the time the data was valid ("as of").

#### Scenario: Known symbol
- **WHEN** an authenticated user requests the fundamentals of a symbol the configured provider knows
- **THEN** the system responds with HTTP 200 and the stock's fundamentals, including the data source and as-of time

#### Scenario: Request without a token
- **WHEN** a client requests a stock's fundamentals without an access token
- **THEN** the system responds with HTTP 401 and returns no data

#### Scenario: Same data for every user
- **WHEN** two different users request the fundamentals of the same symbol
- **THEN** both receive the same data, because market data is not owned by any user

### Requirement: Metrics use fixed units
Monetary values (price, market capitalisation) SHALL be decimal amounts with an ISO 4217 currency code, in the stock's listing currency. Ratios (P/E, debt-to-equity) SHALL be plain decimal numbers. Percentages (EPS growth, institutional ownership, insider ownership) SHALL be decimal fractions, where 0.15 means 15 %.

#### Scenario: Percentage as a fraction
- **WHEN** a provider reports insider ownership of 22 %
- **THEN** the response contains insider ownership 0.22

#### Scenario: Non-USD stock
- **WHEN** a user requests a stock listed in euros
- **THEN** price and market capitalisation carry the currency code `EUR` and are not converted

### Requirement: Missing metrics are reported as not available
The system SHALL report any metric the provider does not supply as absent (null), and SHALL NOT substitute zero or an estimate. The symbol, company name, currency, data source and as-of time are always present.

#### Scenario: Provider lacks a metric
- **WHEN** the provider has no forward P/E for a stock
- **THEN** the response contains a null forward P/E and the other metrics as supplied

#### Scenario: Negative earnings
- **WHEN** a stock has negative earnings, so P/E is not meaningful
- **THEN** the response contains a null P/E rather than a negative or zero value

### Requirement: Ticker symbols are validated and normalised
The system SHALL accept a symbol of 1 to 15 characters made of letters, digits, `.` and `-`, starting with a letter or digit, ignoring surrounding whitespace and letter case. It SHALL return the symbol in upper case.

#### Scenario: Lower-case symbol
- **WHEN** a user requests the symbol `lynch`
- **THEN** the system returns the fundamentals for `LYNCH`

#### Scenario: Invalid symbol
- **WHEN** a user requests a symbol containing other characters, such as `AB$C`, or longer than 15 characters
- **THEN** the system responds with HTTP 400 and does not contact the provider

### Requirement: Unknown symbols are reported as not found
The system SHALL respond with HTTP 404 when the provider does not know the requested symbol.

#### Scenario: Unknown symbol
- **WHEN** a user requests a valid symbol the provider does not know
- **THEN** the system responds with HTTP 404

### Requirement: The provider is chosen through settings
The system SHALL use the market data provider named in its settings, and SHALL refuse to start when the setting names an unknown provider. Changing provider SHALL NOT change the API or the response format.

#### Scenario: Unknown provider configured
- **WHEN** the settings name a provider that does not exist
- **THEN** the application fails at startup with an error naming the setting and the valid providers

#### Scenario: Fake provider configured
- **WHEN** the settings select the fake provider
- **THEN** lookups return its fixed sample data and the data source is reported as the fake provider

### Requirement: Fake provider supplies deterministic sample data
The fake provider SHALL return the same fictional fundamentals for the same symbol on every call, for a fixed set of symbols that includes a stock meeting every Lynch criterion, a stock failing several, a stock with missing metrics, and a stock listed in a currency other than USD.

#### Scenario: Repeatable data
- **WHEN** the same symbol is requested from the fake provider twice
- **THEN** both responses contain the same metric values

### Requirement: Fundamentals are cached for a configurable time
The system SHALL reuse fundamentals fetched from the provider for any user until they are older than the configured maximum age, and only then fetch them again.

#### Scenario: Fresh cached data
- **WHEN** a symbol is requested again before the maximum age has passed since it was fetched
- **THEN** the system returns the cached data without contacting the provider

#### Scenario: Expired cached data
- **WHEN** a symbol is requested after the maximum age has passed since it was fetched
- **THEN** the system fetches the data from the provider again and returns the new data

### Requirement: Stale data is served when the provider fails
When the provider fails or is unreachable, the system SHALL return the most recent cached data for the symbol marked as stale, or respond with HTTP 503 if nothing is cached.

#### Scenario: Provider fails with cached data
- **WHEN** a symbol's cached data has expired and the provider fails
- **THEN** the system responds with HTTP 200, the cached data, its original as-of time, and a stale marker

#### Scenario: Provider fails without cached data
- **WHEN** a symbol has never been fetched and the provider fails
- **THEN** the system responds with HTTP 503

### Requirement: Users can look up a stock in the web app
The web app SHALL let a signed-in user enter a ticker symbol and see its fundamentals with labelled units, the data source and as-of time, a "not available" label for missing metrics, and a visible note when the data is stale.

#### Scenario: Successful lookup
- **WHEN** a signed-in user enters a known symbol and submits
- **THEN** the app shows the company name and every metric, the data source and the as-of time

#### Scenario: Unknown or invalid symbol
- **WHEN** a signed-in user submits an unknown or invalid symbol
- **THEN** the app shows a message saying the symbol was not found or is not valid, and no metrics

#### Scenario: Missing metric shown
- **WHEN** the looked-up stock has no value for a metric
- **THEN** the app shows "not available" for that metric

#### Scenario: Stale data shown
- **WHEN** the response is marked as stale
- **THEN** the app shows a note that the data could not be refreshed, with its as-of time
