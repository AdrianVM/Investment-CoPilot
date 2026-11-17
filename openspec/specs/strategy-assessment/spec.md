# strategy-assessment Specification

## Purpose

Assesses a stock against a named investment strategy, starting with Peter Lynch's six criteria, and reports for each criterion whether the stock meets it, does not meet it, or cannot be assessed, so users can see at a glance how a stock compares with the strategy.

## Requirements

### Requirement: A stock can be assessed against the Peter Lynch strategy
The system SHALL assess a stock, identified by ticker symbol, against the strategy `peter-lynch` for any authenticated user, using the stock's current fundamentals. The result SHALL contain the strategy's name, the stock's symbol and company name, the data source and as-of time, a verdict for each of the strategy's criteria in a fixed order, and a summary.

#### Scenario: Assessing a known stock
- **WHEN** an authenticated user requests the `peter-lynch` assessment of `LYNCH`
- **THEN** the system responds with HTTP 200, six criteria in the order P/E, institutional ownership, insider ownership, EPS growth, debt to equity, market capitalisation, and a summary

#### Scenario: Request without a token
- **WHEN** a client requests an assessment without an access token
- **THEN** the system responds with HTTP 401 and returns no data

#### Scenario: Unknown strategy
- **WHEN** a user requests an assessment for a strategy identifier that does not exist
- **THEN** the system responds with HTTP 404

#### Scenario: Unknown, invalid or unavailable stock
- **WHEN** a user requests an assessment for an unknown symbol, an invalid symbol, or while market data is unavailable with nothing cached
- **THEN** the system responds with HTTP 404, 400 or 503 respectively, as the stock fundamentals lookup does

### Requirement: Each criterion has a verdict with the values behind it
Each criterion SHALL report one verdict, "meets", "does not meet" or "cannot be assessed". It SHALL also report the stock's value for each measure it checks (or null), the comparison and threshold for that measure, and, when it cannot be assessed, the reason.

#### Scenario: Criterion met
- **WHEN** a stock's insider ownership is 0.26
- **THEN** the insider ownership criterion reports "meets", value 0.26, comparison "greater than" and threshold 0.20

#### Scenario: Criterion not met
- **WHEN** a stock's debt to equity is 1.45
- **THEN** the debt to equity criterion reports "does not meet", value 1.45, comparison "less than" and threshold 0.35

### Requirement: Peter Lynch thresholds
The Peter Lynch strategy SHALL use these strict thresholds: trailing P/E less than 25 and forward P/E less than 15; institutional ownership less than 0.10; insider ownership greater than 0.20; EPS growth greater than 0.15; debt to equity less than 0.35; market capitalisation less than 5,000,000,000 USD.

#### Scenario: Value exactly at the threshold
- **WHEN** a stock's EPS growth is exactly 0.15
- **THEN** the EPS growth criterion reports "does not meet", because the threshold is strictly "greater than"

### Requirement: Missing data makes a criterion unassessable, never failed
A criterion whose required value is missing SHALL report "cannot be assessed" with the reason "data not available", and SHALL NOT count as met or not met.

#### Scenario: Missing institutional ownership
- **WHEN** a stock has no institutional ownership value
- **THEN** that criterion reports "cannot be assessed" with the reason "data not available"

#### Scenario: Loss-making company
- **WHEN** a stock has neither a trailing nor a forward P/E because its earnings are negative
- **THEN** the P/E criterion reports "cannot be assessed"

### Requirement: The P/E criterion combines two measures
The P/E criterion SHALL report "meets" only when both trailing and forward P/E are present and below their thresholds. It SHALL report "does not meet" when any present P/E value is at or above its threshold. In all other cases it SHALL report "cannot be assessed".

#### Scenario: Both below
- **WHEN** trailing P/E is 14.2 and forward P/E is 11.8
- **THEN** the P/E criterion reports "meets"

#### Scenario: One present and failing
- **WHEN** trailing P/E is 38.5 and forward P/E is missing
- **THEN** the P/E criterion reports "does not meet"

#### Scenario: One present and passing
- **WHEN** trailing P/E is 19.7 and forward P/E is missing
- **THEN** the P/E criterion reports "cannot be assessed" with the reason "data not available"

### Requirement: Market capitalisation is compared in US dollars
The market capitalisation criterion SHALL compare the market cap in USD. A market cap in another currency SHALL be converted with the current exchange rate, and the criterion SHALL report the original amount, the converted amount, the rate, and the rate's source and as-of time. If no rate is available, the criterion SHALL report "cannot be assessed" with the reason "exchange rate not available".

#### Scenario: Euro listing
- **WHEN** a stock's market cap is 3,200,000,000 EUR and a EUR to USD rate is available
- **THEN** the criterion compares the converted USD amount with the threshold and reports both amounts and the rate used

#### Scenario: Currency without a rate
- **WHEN** a stock's market cap is in a currency for which no exchange rate is available
- **THEN** the criterion reports "cannot be assessed" with the reason "exchange rate not available"

### Requirement: The summary counts verdicts
The summary SHALL state how many criteria are met, not met and cannot be assessed, and the total number of criteria. There SHALL be no combined score.

#### Scenario: Partially assessable stock
- **WHEN** a stock meets 2 criteria, does not meet 1 and 3 cannot be assessed
- **THEN** the summary reports met 2, not met 1, cannot be assessed 3, total 6

### Requirement: Assessments reflect the age of the data
An assessment SHALL carry the data source, as-of time and stale marker of the fundamentals it was computed from, and SHALL NOT be stored.

#### Scenario: Stale fundamentals
- **WHEN** the fundamentals used are marked stale
- **THEN** the assessment is marked stale and shows the fundamentals' as-of time

### Requirement: Users see the assessment in the web app
After a successful stock lookup, the web app SHALL show the Peter Lynch assessment below the stock's figures: the summary, and for each criterion its name, verdict, the stock's value(s) and the threshold, and the reason when it cannot be assessed. Each verdict SHALL be shown by text and icon, not by colour alone. The app SHALL state that the assessment is for information and is not investment advice.

#### Scenario: Assessment shown
- **WHEN** a signed-in user looks up `LYNCH`
- **THEN** the app shows "Meets 6 of 6 criteria" and each criterion marked as met, with its values and threshold

#### Scenario: Converted market cap shown
- **WHEN** a signed-in user looks up a stock listed in EUR
- **THEN** the market capitalisation criterion shows the euro amount, the converted dollar amount and the rate used

#### Scenario: Unassessable criteria shown
- **WHEN** a signed-in user looks up a stock with missing data
- **THEN** the affected criteria show "Cannot be assessed" and the reason, and the summary mentions how many could not be assessed

#### Scenario: Assessment unavailable
- **WHEN** the figures load but the assessment request fails
- **THEN** the figures stay visible and the app shows that the assessment could not be loaded
